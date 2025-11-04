using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Device.State.Logging;
using Ares.Core.Exceptions;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Extensions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Output;
using Ares.Core.Planning;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Execution.Executors;

public class CampaignExecutor : ICampaignExecutor
{
  private readonly IExecutionReporter _executionReporter;
  private readonly ISubject<CampaignExecutionStatus> _executionStatusSubject;
  private readonly ICommandComposer<ExperimentTemplate, ExperimentExecutor> _experimentComposer;
  private readonly IPlanningHelper _planningHelper;
  private readonly IEnumerable<IExecutionSummaryHandler> _summaryHandlers;
  private readonly INotifier _notifier;
  private readonly ILogger _logger;
  private readonly AresVariableManager _variableManager;
  private readonly StateLoggerManager _stateLoggerManager;
  readonly AnalysisHelper _analysisHelper;
  readonly AnalysisRepo _analysisRepo;
  readonly IAnalyzerRepo _analyzerRepo;

  internal CampaignExecutor(ICommandComposer<ExperimentTemplate, ExperimentExecutor> experimentComposer,
    IPlanningHelper planningHelper,
    IExecutionReporter executionReporter,
    AnalysisHelper analysisHelper,
    CampaignTemplate template,
    IEnumerable<IExecutionSummaryHandler> resultHandlers,
    AnalysisRepo analysisRepo,
    INotifier notifier,
    IAnalyzerRepo analyzerRepo,
    ILogger logger,
    AresVariableManager variableManager,
    StateLoggerManager stateLoggerManager)
  {
    _analyzerRepo = analyzerRepo;
    _analysisRepo = analysisRepo;
    _analysisHelper = analysisHelper;
    _variableManager = variableManager;
    _stateLoggerManager = stateLoggerManager;
    _experimentComposer = experimentComposer;
    _planningHelper = planningHelper;
    _executionReporter = executionReporter;
    _summaryHandlers = resultHandlers;
    _logger = logger;
    Template = template;
    _notifier = notifier;

    Status = new CampaignExecutionStatus
    {
      CampaignId = template.UniqueId,
      State = ExecutionState.Waiting
    };

    _executionStatusSubject = new BehaviorSubject<CampaignExecutionStatus>(Status);
    ExperimentStatusObservable = _executionStatusSubject.AsObservable();
  }

  public async Task<CampaignExecutionSummary> Execute(ExecutionControlToken token)
  {
    // Make sure we log all the device state changes during execution.
    await _stateLoggerManager.EnableOverrideAsync(new Datamodel.Device.DeviceLoggingSettings
    {
      LoggingType = Datamodel.Device.DeviceLoggingSettings.Types.LoggingType.OnChange
    });

    try
    {
      var startTime = DateTime.UtcNow;

      //Init Campaign Directories
      var campaignPath = await CampaignOutputHelper.InitializeOutputDirectories(Template, startTime);

      if(!string.IsNullOrEmpty(ExecutionNotes))
        await CampaignOutputHelper.WriteExperimentNotes(campaignPath, ExecutionNotes);

      if(CampaignTags.Any())
        await CampaignOutputHelper.WriteExperimentTags(campaignPath, CampaignTags);

      var analyzerId = string.IsNullOrEmpty(Template.ExperimentTemplate.AnalyzerId) ? NoneAnalyzer.Id : Template.ExperimentTemplate.AnalyzerId;
      if(analyzerId is not null)
      {
        var analyzer = _analyzerRepo.GetAnalyzerById(analyzerId);
        await CampaignOutputHelper.OutputVersionFile(campaignPath, Template, analyzer);
      }

      var experimentSummaries = new List<ExperimentExecutionSummary>();
      var analyses = new List<Analysis>();
      Status = new CampaignExecutionStatus
      {
        CampaignId = Template.UniqueId,
        State = ExecutionState.Waiting
      };

      _analysisRepo.ClearAnalyses();
      Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
      _executionReporter.Report(Status);

      await _notifier.Notify("Campaign Started!", $"ARES has started a campaign named {Template.Name} successfully!", NotificationSeverityEnum.Success);
      bool executionSuccess = true;
      var experiment_count = 0;
      var startupSummary = new ExperimentExecutionSummary();
      if(Template.StartupTemplate is not null)
      {
        var startupExecutorResult = await GenerateExperimentExecutor(Template.StartupTemplate, analyses, experimentSummaries.Select(es => es.ExperimentOverview), token.CancellationToken);
        if(startupExecutorResult.ErrorString is not null || startupExecutorResult.ExperimentExecutor is null)
        {
          await _notifier.Notify("Campaign Failed!", $"ARES failed to run startup routine for {Template.Name}, campaign will shut down.", NotificationSeverityEnum.Error);
          executionSuccess = false;
          return new CampaignExecutionSummary();
        }

        startupSummary = await ExecuteTemplate(startupExecutorResult.ExperimentExecutor, token);
        startupSummary.ResultOutputPath = AresEnvironment.AresEnvironment.GetEnvironmentVariable(VariableType.CampaignStartupFolder);
        await PostExperimentExecution(startupSummary);
      }

      while(!ShouldStop() && !token.IsCancelled && executionSuccess == true)
      {
        var experimentFolder = $"Experiment_{++experiment_count}";
        var experimentPath = CampaignOutputHelper.CreateExperimentSubFolder(campaignPath, experimentFolder);

        //Populate Internal Variables Related to Experiment
        AresEnvironment.AresEnvironment.SetInternalVariable(InternalVariableType.CurrentExperimentNumber, experiment_count.ToString());

        var experimentExecutorResult = await GenerateExperimentExecutor(Template.ExperimentTemplate, analyses, experimentSummaries.Select(es => es.ExperimentOverview), token.CancellationToken);

        if(experimentExecutorResult.ErrorString is not null)
        {
          _logger.LogError("Experiment Failed! Reason: {error-message}", experimentExecutorResult.ErrorString); 
          //await HandleNotification("Experiment Executor Generation Failure", experimentExecutorResult.ErrorString, NotificationSeverityEnum.Error);
          executionSuccess = false;
          break;
        }

        if(experimentExecutorResult.ExperimentExecutor is null)
        {
          await _notifier.Notify("Experiment Executor Generation Failure", "Error was not specified, but the executor generation has failed.", NotificationSeverityEnum.Error);
          executionSuccess = false;
          break;
        }

        var experimentExecutor = experimentExecutorResult.ExperimentExecutor;

        if(experimentExecutorResult.ErrorString is not null)
        {
          await _notifier.Notify("Experiment Executor Generation Failure", experimentExecutorResult.ErrorString, NotificationSeverityEnum.Error);
          executionSuccess = false;
          break;
        }

        var experimentSummary = await ExecuteTemplate(experimentExecutor, token);
        experimentSummary.ResultOutputPath = experimentPath;

        //If a command failed, stop the experiment.
        if(experimentSummary.StepSummaries.Any(step => step.CommandSummaries.Any(cmd => !cmd.Result.Success)) || !experimentSummary.StepSummaries.Any())
        {
          experimentSummaries.Add(experimentSummary);
          executionSuccess = false;
          break;
        }

        // if the execution was canceled, the experiment may not have executed the command to provide the output
        // and thus sending a null result to the analyzer might break it depending on the analyzer
        if(!token.IsCancelled)
        {
          var analysis = await _analysisHelper.Analyze(
            experimentExecutor.Template,
            experimentSummary,
            token.CancellationToken);

          // The following are top level checks for analysis failure in case the
          // failure is not properly handled on the Analysis itself
          // which also has support for "success" and "error" message
          if (analysis is null || analysis.Result == float.NaN)
          {
            await _notifier.Notify("Analysis Failure", $"Analysis was reported as successful, but no actual analysis was provided. {analysis?.ErrorString ?? "No error string provided"}", NotificationSeverityEnum.Error);
            executionSuccess = false;
            break;
          }

          if(analysis.AnalysisOutcome == Outcome.Failure)
          {
            await _notifier.Notify("Analysis Failure", $"Failed to analyze experiment result: {analysis.ErrorString}", NotificationSeverityEnum.Error);
            executionSuccess = false;
            break;           
          }

          else if(analysis.AnalysisOutcome == Outcome.Canceled)
          {
            await _notifier.Notify("Analysis Canceled", "The requested analysis has been canceled", NotificationSeverityEnum.Info);
            break;
          }

          else if(analysis.AnalysisOutcome == Outcome.Warning) 
          { 
            await _notifier.Notify("Warning From Analyzer!", $"Analysis completed successfully, but the analyzer emitted a warning! {analysis.ErrorString}", NotificationSeverityEnum.Warning);
          }

          analyses.Add(analysis);

          _analysisRepo.Add(analysis);
          // Here the analysis has failed, but the analyzer properly reported why
          // it failed via the analysis result.
          if(analysis.ErrorString != string.Empty && string.IsNullOrEmpty(analysis.ErrorString))
          {
            await _notifier.Notify("Analysis Process Failed!", analysis.ErrorString, NotificationSeverityEnum.Error);
            executionSuccess = false;
            break;
          }
        }
        else
        {
          executionSuccess = false;
        }

        await PostExperimentExecution(experimentSummary);
        experimentSummaries.Add(experimentSummary);
      }

      var closeoutSummary = new ExperimentExecutionSummary();
      if(Template.CloseoutTemplate is not null)
      {
        var closeoutExecutorResult = await GenerateExperimentExecutor(Template.CloseoutTemplate, analyses, experimentSummaries.Select(es => es.ExperimentOverview), token.CancellationToken);
        if(closeoutExecutorResult?.ErrorString is not null || closeoutExecutorResult?.ExperimentExecutor is null)
        {
          await _notifier.Notify("Closeout Script Failed!", closeoutExecutorResult?.ErrorString ?? "Unknown Closeout Script Failure", NotificationSeverityEnum.Error);
          executionSuccess = false;
          throw new CloseoutScriptFailedException(closeoutExecutorResult?.ErrorString ?? "Closeout failed, but no reason for failure was provided.");
        }

        closeoutSummary = await ExecuteTemplate(closeoutExecutorResult.ExperimentExecutor, token);
        closeoutSummary.ResultOutputPath = AresEnvironment.AresEnvironment.GetEnvironmentVariable(VariableType.CampaignMiscFolder);
        await PostExperimentExecution(closeoutSummary);
      }

      if(executionSuccess)
      {
        Status.State = ExecutionState.Succeeded;
        await _notifier.Notify("Campaign Completed", $"ARES completed the {Template.Name} campaign successfully.", NotificationSeverityEnum.Success);
      }

      else
      {
        Status.State = ExecutionState.Failed;
        await _notifier.Notify("Campaign Failed", $"ARES failed to execute {Template.Name} successfully, check the event history page for errors.", NotificationSeverityEnum.Error);
      }
      

      _executionReporter.Report(Status);

      var campaignExecutionSummary = new CampaignExecutionSummary
      {
        UniqueId = Guid.NewGuid().ToString(),
        CampaignId = Template.UniqueId,
        ExecutionInfo = new ExecutionInfo
        {
          Timezone = TimeZoneInfo.Local.DisplayName,
          LocaltimeOffset = DateTimeOffset.Now.Offset.ToString(),
          TimeFinished = DateTime.UtcNow.ToTimestamp(),
          TimeStarted = startTime.ToUniversalTime().ToTimestamp()
        }
      };

      campaignExecutionSummary.ExperimentSummaries.AddRange(experimentSummaries);
      campaignExecutionSummary.StartupExecutionSummary = startupSummary;
      campaignExecutionSummary.CloseoutExecutionSummary = closeoutSummary;
      ExecutionNotes = string.Empty;

      return campaignExecutionSummary;
    }
    finally
    {
      await _stateLoggerManager.DisableOverrideAsync();
    }
  }

  private bool ShouldStop()
  {
    return StopConditions.Any(condition => condition.ShouldStop());
  }

  public void UpdateExecutionNotes(string notes) => ExecutionNotes = notes;

  public void UpdateCampaignTags(List<AresCampaignTag> tags) => CampaignTags = tags;

  private bool IsAwaitingResponse(ExperimentExecutionStatus status)
    => status.StepExecutionStatuses
    .Any(step => step.CommandExecutionStatuses
    .Any(cmd => cmd.State == ExecutionState.AwaitingUser));

  private async Task<ExperimentExecutorResult> GenerateExperimentExecutor(ExperimentTemplate template, IEnumerable<Analysis> analyses, IEnumerable<ExperimentOverview> previousExperiments, CancellationToken cancellationToken)
  {
    var result = new ExperimentExecutorResult();
    var experimentTemplate = template.CloneWithNewIds();

    if(!experimentTemplate.IsResolved())
    {
      if(analyses.Count() % ReplanRate == 0)
      {
        var resolveSuccess = await _planningHelper.TryResolveParameters(Template.PlannerAllocations, Template.UniqueId, experimentTemplate.GetAllPlannedParameters(), analyses, previousExperiments, cancellationToken);
        if(!resolveSuccess)
        {
          result.ErrorString = "Failed to plan! Experiment will be terminated!";
          return result;
        }
      }

      else
        experimentTemplate = previousExperiments.Last().Template.CloneWithNewIds();
    }

    if(!experimentTemplate.IsEnvironmentResolved())
    {
      var resolveVarsSuccess = _variableManager.TryResolveVariable(experimentTemplate.GetAllParameters());

      if(!resolveVarsSuccess)
      {
        result.ErrorString = "Failed to assign environment variables! Experiment will be terminated!";
        return result;
      }
    }

    //Passing the campaigns name into the experiment template for file creation purposes post experiment
    experimentTemplate.Name = Template.Name;

    result.ExperimentExecutor = _experimentComposer.Compose(experimentTemplate);
    return result;
  }

  private async Task<ExperimentExecutionSummary> ExecuteTemplate(ExperimentExecutor experimentExecutor, ExecutionControlToken token)
  {
    Status.ExperimentExecutionStatuses.Add(experimentExecutor.Status);
    experimentExecutor.ExperimentStatusObservable.Subscribe(experimentStatus =>
    {
      _executionReporter.Report(experimentStatus);

      if(IsAwaitingResponse(experimentStatus))
        Status.State = ExecutionState.AwaitingUser;
      else
        Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;

      _executionStatusSubject.OnNext(Status);
      _executionReporter.Report(Status);
    });

    return await experimentExecutor.Execute(token);
  }

  private async Task PostExperimentExecution(ExperimentExecutionSummary summary)
  {
    foreach(var handler in _summaryHandlers)
    {
      await handler.Handle(summary);
    }
  }

  public CampaignTemplate Template { get; }
  public IList<IStopCondition> StopConditions { get; } = [];
  public double ReplanRate { get; set; } = 1;
  public string? ExecutionNotes { get; set; }
  public List<AresCampaignTag> CampaignTags { get; set; } = [];
  public IObservable<CampaignExecutionStatus> ExperimentStatusObservable { get; }
  public CampaignExecutionStatus Status { get; private set; }
}
