using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Core.Execution;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Grpc.Services;

public class AutomationService : AresAutomation.AresAutomationBase
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IDbContextFactory<CoreDatabaseContext> _coreContextFactory;
  private readonly IExecutionManager _executionManager;
  private readonly IExecutionReportStore _executionReportStore;
  private readonly IEnumerable<IStartCondition> _startConditions;
  private readonly IEnumerable<INotificationHandler> _notificationHandlers;
  readonly IDesiredAnalysisResultFactory _desiredAnalysisResultFactory;
  private JsonSerializerOptions _serializerSettings;
  readonly IAnalyzerRepo _analyzerRepo;

  public AutomationService(IDbContextFactory<CoreDatabaseContext> coreContextFactory,
    IExecutionManager executionManager,
    IExecutionReportStore executionReportStore,
    IActiveCampaignTemplateStore activeCampaignTemplateStore,
    IEnumerable<IStartCondition> startConditions,
    IAnalyzerRepo analyzerRepo,
    IEnumerable<INotificationHandler> notificationHandlers,
    IDesiredAnalysisResultFactory desiredAnalysisResultFactory)
  {
    _analyzerRepo = analyzerRepo;
    _desiredAnalysisResultFactory = desiredAnalysisResultFactory;
    _coreContextFactory = coreContextFactory;
    _executionManager = executionManager;
    _executionReportStore = executionReportStore;
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _startConditions = startConditions;
    _serializerSettings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    _notificationHandlers = notificationHandlers;
  }

  public override async Task<ProjectsResponse> GetAllProjects(Empty request, ServerCallContext context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var projects = await dbContext.Projects.AsNoTracking().ToArrayAsync(context.CancellationToken);
    var response = new ProjectsResponse();
    response.Projects.AddRange(projects);
    return response;
  }

  public override async Task<CampaignsResponse> GetAllCampaigns(GetAllCampaignsRequest request, ServerCallContext context)
  {
    var campaignResponse = new CampaignsResponse();
    foreach(var file in Directory.EnumerateFiles(AresConfig.TemplatePath, "*.json"))
    {
      try
      {
        var contents = await File.ReadAllTextAsync(file);
        var campaignTemplate = JsonSerializer.Deserialize<CampaignTemplate>(contents, _serializerSettings);
        if(campaignTemplate is not null)
          campaignResponse.CampaignTemplates.Add(campaignTemplate);

        else
          throw new Exception("Deserialization of campaign template failed");
      }

      catch(Exception ex)
      {
        HandleNotification("Error Loading Campaign Template", $"{file} - {ex.Message}", NotificationSeverityEnum.Error);
      }
    }

    var blah = campaignResponse.CalculateSize();
    return campaignResponse;
  }

  public override Task<BoolValue> CampaignExists(CampaignRequest request, ServerCallContext context)
  {
    if(request.HasUniqueId)
      return Task.FromResult(FindCampaignById(request));

    else
      return Task.FromResult(FindCampaignByName(request));
  }

  private BoolValue FindCampaignById(CampaignRequest request)
  {
    var directoryFiles = Directory.EnumerateFiles(AresConfig.TemplatePath, "*.json");

    if(directoryFiles.Any(file => file.Contains(request.UniqueId)))
      return new BoolValue { Value = true };

    return new BoolValue { Value = false };
  }

  private BoolValue FindCampaignByName(CampaignRequest request)
  {
    var directoryFiles = Directory.EnumerateFiles(AresConfig.TemplatePath, "*.json");

    foreach(var file in directoryFiles)
    {
      var jsonString = File.ReadAllText(Path.Combine(AresConfig.TemplatePath, file));
      var templateObject = JsonSerializer.Deserialize<CampaignTemplate>(jsonString, _serializerSettings);
      if(templateObject is not null && templateObject.Name == request.CampaignName)
        return new BoolValue { Value = true };
    }

    return new BoolValue { Value = false };
  }

  public override Task<CampaignTemplate?> GetSingleCampaign(CampaignRequest request, ServerCallContext context)
  => GetCampaignTemplate(request, context);

  public override Task<Empty> RemoveCampaign(CampaignRequest request, ServerCallContext context)
  {
    var desiredCampaign = Directory.EnumerateFiles(AresConfig.TemplatePath, "*.json").FirstOrDefault(campaign => campaign.Contains(request.UniqueId));

    if(desiredCampaign is not null)
      File.Delete(Path.Combine(AresConfig.TemplatePath, desiredCampaign));

    return Task.FromResult(new Empty());
  }

  public override async Task<Project> GetProject(ProjectRequest request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    return await dbContext.Projects.AsNoTracking().FirstAsync(project => project.Name == request.ProjectName, context.CancellationToken);
  }

  public override async Task<Empty> RemoveProject(ProjectRequest request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    var project = await dbContext.Projects.FirstAsync(p => p.Name == request.ProjectName, context.CancellationToken);
    dbContext.Projects.Remove(project);
    await dbContext.SaveChangesAsync(context.CancellationToken);

    return new Empty();
  }

  public override async Task<Empty> AddProject(Project request, ServerCallContext context)
  {
    using var dbContext = _coreContextFactory.CreateDbContext();
    dbContext.Projects.Add(request);
    await dbContext.SaveChangesAsync(context.CancellationToken);
    return new Empty();
  }

  /// <summary>
  /// </summary>
  /// <param name="request">
  ///   <see
  /// </param>
  /// <param name="context"></param>
  /// <returns></returns>
  public override Task<Empty> AddCampaign(AddOrUpdateCampaignRequest request, ServerCallContext context)
  {
    //Save to data directory
    var directoryFiles = Directory.EnumerateFiles(AresConfig.TemplatePath, "*.json");
    var jsonString = JsonSerializer.Serialize(request.Template, _serializerSettings);
    var fullFilePath = Path.Combine(AresConfig.TemplatePath, $"{request.Template.UniqueId}.json");
    File.WriteAllText(fullFilePath, jsonString);
    return Task.FromResult(new Empty());
  }

  public override Task<CampaignTemplate> UpdateCampaign(AddOrUpdateCampaignRequest request, ServerCallContext context)
  {
    var directoryFiles = Directory.EnumerateFiles(AresConfig.TemplatePath, "*.json");
    var campaignToUpdate = directoryFiles.FirstOrDefault(file => file.Contains(request.Template.UniqueId));

    if(campaignToUpdate is null)
    {
      var title = "Error Updating Campaign";
      var message = $"Attempted to update a campaign that didn't exist. {request.Template.Name} couldn't be found in your list of available campaign templates.";
      HandleNotification(title, message, NotificationSeverityEnum.Error);
      return Task.FromResult(request.Template);
    }

    var jsonString = JsonSerializer.Serialize(request.Template, _serializerSettings);
    var fullPath = Path.Combine(AresConfig.TemplatePath, $"{request.Template.UniqueId}.json");
    File.WriteAllText(fullPath, jsonString);
    return Task.FromResult(request.Template);
  }

  private async Task<CampaignTemplate?> GetCampaignTemplate(CampaignRequest request, ServerCallContext context)
  {
    var directoryFiles = Directory.EnumerateFiles(AresConfig.TemplatePath, "*.json");
    var campaignFile = directoryFiles.FirstOrDefault(file => file.Contains(request.UniqueId));

    if(campaignFile is not null)
    {
      var jsonString = await File.ReadAllTextAsync(Path.Combine(AresConfig.TemplatePath, campaignFile));
      var campaignObject = JsonSerializer.Deserialize<CampaignTemplate>(jsonString, _serializerSettings);

      if(campaignObject is not null)
        return campaignObject;
    }

    var title = "Error Fetching Campaign Template";
    var message = $"Attempted to fetch a campaign that didn't exist. {request.CampaignName}'s UUID did not match any of the existing campaigns in your data directory";
    HandleNotification(title, message, NotificationSeverityEnum.Error);
    return null;
  }

  public override Task<CampaignResponse> GetCurrentlySelectedCampaign(Empty request, ServerCallContext context)
  {
    var template = _activeCampaignTemplateStore.CampaignTemplate;
    var response = new CampaignResponse { HasValue = template is not null, Value = template };
    return Task.FromResult(response);
  }

  public override Task<Empty> StartExecution(StartCampaignRequest request, ServerCallContext context)
  {
    _executionManager.Start(request.UserNotes, request.CampaignTags.ToList());
    return Task.FromResult(new Empty());
  }

  public override async Task<CampaignTemplate> SetCampaignForExecution(CampaignRequest request, ServerCallContext context)
  {
    var template = await GetCampaignTemplate(request, context);
    if(template is null)
    {
      throw new InvalidOperationException($"No campaign template found for request. Name: {request.CampaignName}");
    }
    _activeCampaignTemplateStore.CampaignTemplate = template;
    return template;
  }

  public override Task GetExecutionStatusStream(Empty request, IServerStreamWriter<ExperimentExecutionStatus> responseStream, ServerCallContext context)
  {
    var observable = _executionReportStore.ExperimentStatusObservable;
    return observable.Where(status => status is not null).Do(status => responseStream.WriteAsync(status!)).ToTask(context.CancellationToken);
  }

  public override Task GetStartupExecutionStatusStream(Empty request, IServerStreamWriter<CampaignStartupStatus> responseStream, ServerCallContext context)
  {
    var observable = _executionReportStore.CampaignStartupStatusObservable;
    return observable.Where(status => status is not null).Do(status => responseStream.WriteAsync(status!)).ToTask(context.CancellationToken);
  }

  public override Task GetCloseoutExecutionStatusStream(Empty request, IServerStreamWriter<CampaignCloseoutStatus> responseStream, ServerCallContext context)
  {
    var observable = _executionReportStore.CampaignCloseoutStatusObservable;
    return observable.Where(status => status is not null).Do(status => responseStream.WriteAsync(status!)).ToTask(context.CancellationToken);
  }

  public override Task<CampaignExecutionStatusResponse> GetCampaignExecutionStatus(Empty request, ServerCallContext context)
  {
    var status = _executionReportStore.CampaignExecutionStatus;
    return Task.FromResult(new CampaignExecutionStatusResponse
    {
      Status = status
    });
  }

  public override Task GetCampaignExecutionStateStream(Empty request, IServerStreamWriter<CampaignExecutionState> responseStream, ServerCallContext context)
  {
    var observable = _executionReportStore.CampaignStatusObservable;
    return observable!
      .OfType<CampaignExecutionStatus>()
      .Select(status => new CampaignExecutionState { CampaignId = status.CampaignId, State = status.State })
      .Do(state => responseStream.WriteAsync(state))
      .ToTask(context.CancellationToken);
  }

  public override Task<Empty> StopExecution(Empty request, ServerCallContext context)
  {
    _executionManager.Stop();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> PauseExecution(Empty request, ServerCallContext context)
  {
    _executionManager.Pause();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> ResumeExecution(Empty request, ServerCallContext context)
  {
    _executionManager.Resume();
    return Task.FromResult(new Empty());
  }

  public override Task<StartStopConditionsResponse> GetAssignedStopConditions(Empty request, ServerCallContext context)
  {
    var conditions = _executionManager.CampaignStopConditions;
    var response = new StartStopConditionsResponse();
    var startStopConditions = conditions?.Select(condition => new StartStopCondition { Message = condition.Message, Name = condition.GetType().Name }) ?? new List<StartStopCondition>();
    response.StartStopConditions.AddRange(startStopConditions);

    return Task.FromResult(response);
  }

  public override async Task<StartStopConditionsResponse> GetFailedStartConditions(Empty request, ServerCallContext context)
  {
    var response = new StartStopConditionsResponse();
    var conditionResults = await Task.WhenAll(_startConditions.Select(condition => condition.CanStart()));
    var conditions = conditionResults.Where(result => result is not null && !result.Success).Select(condition => new StartStopCondition { Message = string.Join(Environment.NewLine, condition!.Messages), Name = condition.GetType().Name });
    response.StartStopConditions.AddRange(conditions);

    return response;
  }

  public override Task<Empty> RemoveStopCondition(StartStopCondition request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null)
      return Task.FromResult(new Empty());

    var condition = stopConditions.FirstOrDefault(condition => condition.GetType().Name.Equals(request.Name));
    if(condition is not null)
      stopConditions.Remove(condition);

    return Task.FromResult(new Empty());
  }

  public override async Task<StartStopConditionsResponse> GetPreliminaryFailedStartConditions(CampaignTemplate request, ServerCallContext context)
  {
    var response = new StartStopConditionsResponse();
    var conditionResults = await Task.WhenAll(_startConditions.Select(condition => condition.CanStart()));
    var conditions = conditionResults.Where(result => result is not null && !result.Success).Select(condition => new StartStopCondition { Message = string.Join(Environment.NewLine, condition!.Messages), Name = condition.GetType().Name });
    response.StartStopConditions.AddRange(conditions);

    return response;
  }

  private async Task<IEnumerable<StartConditionResult>> GetFailedStartConditionResults()
  {
    var conditionTasks = _startConditions.Select(condition => condition.CanStart());
    var conditions = await Task.WhenAll(conditionTasks);
    return conditions;
  }

  public override Task<Empty> SetNumExperimentsStopCondition(NumExperimentsCondition request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null)
      return Task.FromResult(new Empty());

    stopConditions.Clear();

    var newCondition = new NumExperimentsRun(_executionReportStore, request.NumExperiments);
    stopConditions.Add(newCondition);

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetReplanRate(ReplanRate request, ServerCallContext context)
  {
    _executionManager.UpdateReplanRate(request.ReplanRate_);
    return Task.FromResult(new Empty());
  }

  public override Task<GetReplanRateResponse> GetReplanRate(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new GetReplanRateResponse { ReplanRate = _executionManager.ReplanRate });
  }

  public override Task<Empty> SetAnalysisResultStopCondition(AnalysisResultCondition request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null)
      return Task.FromResult(new Empty());

    stopConditions.Clear();

    var stopCondition = _desiredAnalysisResultFactory.Create(request.DesiredResult, request.Leeway);
    stopConditions.Add(stopCondition);

    return Task.FromResult(new Empty());
  }

  public override Task<ExperimentStopConditionResponse> GetActiveStopCondition(Empty request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null || !stopConditions.Any())
    {
      return Task.FromResult(
        new ExperimentStopConditionResponse
        {
          ActiveCondition = "None",
          Description = "No stop conditions assigned, experiment will run until manually stopped."
        });
    }

    var condition = stopConditions.First();
    return Task.FromResult(
      new ExperimentStopConditionResponse
      {
        ActiveCondition = condition.GetType().Name,
        Description = condition.Description
      });
  }

  public override async Task<CheckExecutionEligibilityResponse> CheckExecutionEligibility(Empty request, ServerCallContext context)
  {
    var eligbilityError = await _executionManager.CheckCampaignStartPrerequisites();

    if(string.IsNullOrEmpty(eligbilityError))
      return new CheckExecutionEligibilityResponse { Error = string.Empty, IsEligible = true };

    else
      return new CheckExecutionEligibilityResponse { Error = eligbilityError, IsEligible = false };
  }

  public override async Task<TagsResponse> GetAllTags(Empty request, ServerCallContext context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var existingTags = await dbContext.CampaignTags.ToArrayAsync();
    var response = new TagsResponse();
    response.AvailableTags.AddRange(existingTags);
    return response;
  }

  public override async Task<TagsResponse> AddTag(TagRequest request, ServerCallContext context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var existingTags = await dbContext.CampaignTags.ToArrayAsync();
    if(existingTags.Any(t => t.UniqueId == request.Tag.UniqueId))
      //Duplicate tag, don't do it plz
      throw new InvalidOperationException();

    dbContext.CampaignTags.Add(request.Tag);
    await dbContext.SaveChangesAsync();

    var response = new TagsResponse();
    response.AvailableTags.AddRange(existingTags);
    response.AvailableTags.Add(request.Tag);
    return response;
  }

  public override async Task<TagsResponse> RemoveTag(TagRequest request, ServerCallContext context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var existingTags = await dbContext.CampaignTags.ToArrayAsync();
    var match = existingTags.FirstOrDefault(tag => tag.UniqueId == request.Tag.UniqueId);

    if(match is not null)
    {
      dbContext.Remove(match);
      await dbContext.SaveChangesAsync();
    }

    var response = new TagsResponse();
    response.AvailableTags.AddRange(await dbContext.CampaignTags.ToArrayAsync());
    return response;
  }

  public override async Task<AvailableCampaignExecutionSummariesResponse> GetAvailableCampaignExecutionSummaries(Empty request, ServerCallContext context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var summaries = await dbContext.CampaignExecutionSummaries
      .AsNoTracking()
      .AsSplitQuery()
      .ToArrayAsync(context.CancellationToken);
    var response = new AvailableCampaignExecutionSummariesResponse();
    response.AvailableCampaignSummaries
      .AddRange(summaries
      .Select(summary => new CampaignExecutionSummaryMetadata
      {
        CampaignName = summary.CampaignName,
        CompletionTime = summary.ExecutionInfo.TimeFinished,
        SummaryId = summary.UniqueId,
        NumExperiments = summary.ExperimentSummaries.Count
      }));

    return response;
  }

  public override async Task<CampaignExecutionSummary> GetCampaignSummary(CampaignExecutionSummaryRequest request, ServerCallContext context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    //Include.... EVERYTHING!
    var summary = await dbContext.CampaignExecutionSummaries
      .AsNoTracking()
      .AsSplitQuery()
      .FirstOrDefaultAsync(s => s.UniqueId == request.SummaryId, context.CancellationToken);

    if(summary is null)
      //TODO: Figure out what to do here..?
      throw new InvalidOperationException("Couldn't locate a matching campaign summary!");

    return summary;
  }

  private void HandleNotification(string title, string message, NotificationSeverityEnum severity)
  {
    foreach(var handler in _notificationHandlers)
    {
      handler.HandleNotification(title, message, severity);
    }
  }
}
