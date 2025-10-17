using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using MK4S.Services;
using PrusaMK4S.Enums;
using Radzen;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using UI.Backend.Extensions;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Automation;

public class ExecutionViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly AresAnalyzerManagementService.AresAnalyzerManagementServiceClient _analyzerService;
  public readonly ObservableCollection<CampaignTemplateSummary> CampaignTemplateSummaries = new();
  private readonly INotificationReceivingService _notificationService;
  private readonly MK4SPrinterRpc.MK4SPrinterRpcClient _printerClient;
  private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
  private Task _campaignStatusListener = Task.CompletedTask;

  public ExecutionViewModel(AresAutomation.AresAutomationClient automationClient,
    IConfiguration configuration,
    INotificationReceivingService notificationService,
    AresAnalyzerManagementService.AresAnalyzerManagementServiceClient analyzerService,
    MK4SPrinterRpc.MK4SPrinterRpcClient printerClient)
  {
    _automationClient = automationClient;
    _notificationService = notificationService;
    _analyzerService = analyzerService;
    _printerClient = printerClient;
  }

  public async Task<bool> EnsureStopConditionSet()
  {
    await GetCurrentStopCondition();
    return CurrentStopCondition is not null;
  }

  public async Task RefreshCampaigns()
  {
    var campaigns = await _automationClient.GetAllCampaignsAsync(new GetAllCampaignsRequest());
    CampaignTemplateSummaries.Clear();
    CampaignTemplateSummaries.AddRange(campaigns.Campaigns);
  }

  public async Task SelectCampaignTemplate(object? templateSummary)
  {
    if(templateSummary is null || templateSummary is not CampaignTemplateSummary campaignTemplateSummary)
      return;

    CampaignTemplate = await _automationClient.GetSingleCampaignAsync(new CampaignRequest { UniqueId = campaignTemplateSummary.UniqueId });
    await _automationClient.SetCampaignForExecutionAsync(new CampaignRequest { UniqueId = CampaignTemplate.UniqueId });
    _ = UpdateCurrentTemplate();

    if(SmartPrintCalculation)
      await SetSmartExperimentsToRun();
  }

  public async Task UpdateCurrentTemplate()
  {
    var currentTemplateOpt = await _automationClient.GetCurrentlySelectedCampaignAsync(new Empty());
    CampaignTemplate = currentTemplateOpt.Value;
    if(CampaignTemplate is null)
      return;

    PlannerAdapterInfos = CampaignTemplate.ExperimentTemplate.GetAllPlannedParameters()
    .Select(parameter => parameter.PlanningMetadata)
    .Select(metadata => CampaignTemplate.PlannerAllocations
    .FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner).ToHashSet();
    
    var analyzerId = CampaignTemplate.ExperimentTemplate.AnalyzerId;
    
    if(analyzerId is not null)
    {
      var request = new AnalyzerInfoRequest();
      request.AnalyzerId = analyzerId;
      var response = await _analyzerService.GetInfoAsync(request);
      AnalyzerInfo = response.Info;
    }
  }

  public async Task SetDesiredAnalysis()
  {
    await _automationClient.SetAnalysisResultStopConditionAsync(
      new AnalysisResultCondition { DesiredResult = DesiredResult, Leeway = DesiredLeeway }).ResponseAsync;
    CurrentStopCondition = await GetCurrentStopCondition();
  }

  public Task<ExperimentStopConditionResponse> GetCurrentStopCondition()
  {
    return _automationClient.GetActiveStopConditionAsync(new Empty()).ResponseAsync;
  }

  public Task<GetReplanRateResponse> GetCurrentReplanRate()
  {
    return _automationClient.GetReplanRateAsync(new Empty()).ResponseAsync;
  }

  public async Task<CampaignExecutionStatus?> GetCampaignExecutionStatus()
  {
    var response = await _automationClient.GetCampaignExecutionStatusAsync(new Empty());
    return response.Status;
  }

  public async Task SetExperimentsToRun()
  {
    await _automationClient.SetNumExperimentsStopConditionAsync(new NumExperimentsCondition { NumExperiments = ExperimentsToRun });
    CurrentStopCondition = await GetCurrentStopCondition();
  }

  public async Task SetSmartExperimentsToRun()
  {
    if(CampaignTemplate is null)
      return;

    var stepTemplatesWithPrint = CampaignTemplate.ExperimentTemplate.StepTemplates.Where(step => step.CommandTemplates.Any(cmd => cmd.Metadata.Name == "Print"));
    
    if(stepTemplatesWithPrint.Count() > 1 || !stepTemplatesWithPrint.Any())
    {
      var notification = new AresNotification();
      notification.Title = "Cannot Activate Smart Print";
      notification.Message = "Smart Print can only be used for campaigns that have a single print command.";
      notification.NotificationSeverity = Severity.Warning;
      _notificationService.PushNotification(notification);
      SmartPrintCalculation = false;
      return;
    }

    var cmd = stepTemplatesWithPrint.First().CommandTemplates.First(cmd => cmd.Metadata.Name == "Print");

    if(SmartPrintCalculation)
    {
      var printBytes = cmd.Parameters.First(p => p.Metadata.Name.Equals(PrusaMK4SCommandParameter.GCode.ToString())).Value.BytesValue;
      var request = new CalculateNumberOfPrintsRequest() { Gcode = printBytes, Id = cmd.Metadata.DeviceId };
      var response = await _printerClient.CalculateNumberOfPrintsAsync(request);

      ExperimentsToRun = response.NumberOfExperiments;

      await _automationClient.SetNumExperimentsStopConditionAsync(new NumExperimentsCondition { NumExperiments = ExperimentsToRun });
      CurrentStopCondition = await GetCurrentStopCondition();
    }

    await _printerClient.SetSmartPrintModeAsync(new SmartPrintModeRequest() { Id = cmd.Metadata.DeviceId, ShouldSmartPrint = SmartPrintCalculation });
  }

  public async Task SetReplanRate()
  {
    await _automationClient.SetReplanRateAsync(new ReplanRate { ReplanRate_ = DesiredReplanRate });
    var blah = await GetCurrentReplanRate();
    DesiredReplanRate = blah.ReplanRate;
  }

  public Task StopCampaign()
    => _automationClient.StopExecutionAsync(new Empty()).ResponseAsync;

  public Task PauseCampaign()
    => _automationClient.PauseExecutionAsync(new Empty()).ResponseAsync;

  public Task ResumeCampaign()
    => _automationClient.ResumeExecutionAsync(new Empty()).ResponseAsync;

  public async Task ExecutionNotesUploaded(UploadChangeEventArgs args)
  {
    var maxFileSize = 100;
    var file = args.Files.First();

    using(var stream = file.OpenReadStream(maxFileSize))
    using(var reader = new StreamReader(stream))
    {
      try
      {
        ExecutionNotes = await reader.ReadToEndAsync();
      }

      catch(Exception ex)
      {
        var notification = new AresNotification();
        notification.NotificationSeverity = Severity.Error;
        notification.Title = "Failed to Upload Experiment Notes";
        notification.Message = $"ARES failed to read the uploaded experiment notes file. {ex.Message}";
        notification.Timestamp = DateTime.UtcNow.ToTimestamp();

        _notificationService.PushNotification(notification);
      }
    }
  }

  public Task ReqeustUserConfirmation()
  {
    var notification = new AresNotification();
    notification.NotificationSeverity = Severity.Info;
    notification.Title = "User Confirmation Required to Proceed";
    notification.Message = $"ARES has paused it's current experiment awaiting user input. Press the play button to continue experimenting.";
    notification.Timestamp = DateTime.UtcNow.ToTimestamp();
    notification.Loiter = true;

    _notificationService.PushNotification(notification);

    return Task.CompletedTask;
  }

  public async Task AddTag()
  {
    if(NewTagName is not null && AvailableTags.Any(t => t.TagName == NewTagName))
    {
      var notification = new AresNotification();
      notification.NotificationSeverity = Severity.Info;
      notification.Title = $"Could Not Add {NewTagName} Tag";
      notification.Message = "ARES could not add a new experiment tag because it matched one that already existed!";
      notification.Timestamp = DateTime.UtcNow.ToTimestamp();
      _notificationService.PushNotification(notification);
      return;
    }

    var newProtoTag = new AresCampaignTag { TagName = NewTagName, UniqueId = Guid.NewGuid().ToString() };
    var currentTagCount = AvailableTags.Count;
    var request = new TagRequest();
    request.Tag = newProtoTag;
    var tags = await _automationClient.AddTagAsync(request);

    if(tags.AvailableTags.Count == currentTagCount + 1)
    {
      var notification = new AresNotification();
      notification.NotificationSeverity = Severity.Success;
      notification.Title = $"Successfully Added {NewTagName} Tag";
      notification.Message = "ARES has successfully added a new experiment tag, and it is now available for use";
      notification.Timestamp = DateTime.UtcNow.ToTimestamp();
      _notificationService.PushNotification(notification);
    }

    else
    {
      var notification = new AresNotification();
      notification.NotificationSeverity = Severity.Error;
      notification.Title = $"Failed to Add {NewTagName} Tag";
      notification.Message = "ARES failed to add a new experiment tag";
      notification.Timestamp = DateTime.UtcNow.ToTimestamp();
      _notificationService.PushNotification(notification);
    }

    AvailableTags = tags.AvailableTags.ToList();
    NewTagName = string.Empty;
  }

  public async Task RemoveTag(AresCampaignTag? aresTag)
  {
    if(aresTag is null)
      return;

    var request = new TagRequest() { Tag = aresTag };
    var tags = await _automationClient.RemoveTagAsync(request);

    AvailableTags = tags.AvailableTags.ToList();

    if(SelectedTags.Contains(aresTag))
      SelectedTags.Remove(aresTag);
  }

  public async Task GetAllTags()
  {
    var tags = await _automationClient.GetAllTagsAsync(new Empty());
    AvailableTags = tags.AvailableTags.ToList();
  }

  [Reactive]
  public ExperimentStopConditionResponse? CurrentStopCondition { get; set; }
  public double DesiredResult { get; set; }
  public double DesiredLeeway { get; set; }
  public int DesiredReplanRate { get; set; } = 1;
  [Reactive]
  public bool CampaignActive { get; set; }
  [Reactive]
  public bool CampaignPaused { get; set; }
  [Reactive]
  public CampaignTemplateSummary SelectedTemplateSummary { get; set; }
  [Reactive]
  public CampaignTemplate? CampaignTemplate { get; set; }
  [Reactive]
  public ExecutionState? CampaignExecutionState { get; set; }
  [Reactive]
  public ExperimentExecutionStatus? ExperimentStatus { get; private set; }
  [Reactive]
  public HashSet<PlannerServiceInfo?> PlannerAdapterInfos { get; set; } = new();
  [Reactive]
  public bool SmartPrintCalculation { get; set; }
  [Reactive]
  public AnalyzerInfo? AnalyzerInfo { get; set; }
  public uint ExperimentsToRun { get; set; }
  public string ExecutionNotes { get; set; } = string.Empty;
  public CampaignExecutionSummary? TestCampaignExecutionSummary { get; private set; }
  public IEnumerable<CampaignExecutionSummaryMetadata>? TestCampaignResultMetadata { get; private set; }
  public bool DisplayExecutionSummary { get; set; }
  public List<AresCampaignTag> AvailableTags { get; set; } = new();
  public List<AresCampaignTag> SelectedTags { get; set; } = new();
  public string? NewTagName { get; set; }
}
