using Ares.Core.Exceptions;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Safety;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using DynamicData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Execution;

public class ExecutionManager : IExecutionManager
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly ICommandComposer<CampaignTemplate, ICampaignExecutor> _campaignComposer;
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IEnumerable<IStartCondition> _startConditions;
  private readonly IExecutionSafetyManager _safetyManager;
  private readonly INotifier _notifier;
  private readonly ILogger _logger;
  private ExecutionControlTokenSource? _executionControlTokenSource;

  public ExecutionManager(IEnumerable<IStartCondition> startConditions,
    IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    IActiveCampaignTemplateStore activeCampaignTemplateStore,
    IExecutionSafetyManager safetyManager,
    ICommandComposer<CampaignTemplate, ICampaignExecutor> campaignComposer,
    ILogger<ExecutionManager> logger,
    INotifier notifier)
  {
    _startConditions = startConditions;
    _dbContextFactory = dbContextFactory;
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _campaignComposer = campaignComposer;
    _safetyManager = safetyManager;
    _logger = logger;
    _notifier = notifier;
  }

  public IList<IStopCondition> CampaignStopConditions { get; } = [];

  public async Task<bool> CanRun()
  {
    if(_activeCampaignTemplateStore.CampaignTemplate is null)
      return false;

    var startConditionTasks = _startConditions.Select(sc => sc.CanStart());
    var startConditions = await Task.WhenAll(startConditionTasks);
    return startConditions.All(condition => condition?.Success ?? true);
  }
  public int ReplanRate { get; private set; } = 1;
  public async Task Start(string executionNotes, List<AresCampaignTag> campaignTags)
  {
    var err = await CheckCampaignStartPrerequisites();
    if(!string.IsNullOrEmpty(err))
    {
      throw new InvalidOperationException(err);
    }
    var executor = _campaignComposer.Compose(_activeCampaignTemplateStore.CampaignTemplate!);

    if(!string.IsNullOrEmpty(executionNotes))
      executor.UpdateExecutionNotes(executionNotes);

    if(campaignTags.Any())
      executor.UpdateCampaignTags(campaignTags);

    executor.StopConditions.Add(CampaignStopConditions);
    executor.ReplanRate = ReplanRate;
    _executionControlTokenSource = new ExecutionControlTokenSource();
    CampaignExecutionSummary campaignExecutionSummary;

    try
    {
      campaignExecutionSummary = await executor.Execute(_executionControlTokenSource.Token);
      campaignExecutionSummary.CampaignName = _activeCampaignTemplateStore.CampaignTemplate!.Name;
      campaignExecutionSummary.CampaignNotes = executionNotes;
      campaignExecutionSummary.CampaignTags = string.Join(",", campaignTags.Select(tag => tag.TagName).ToList());

      await PostExecution(campaignExecutionSummary);
    }
    catch(CloseoutScriptFailedException ex)
    {
      await _safetyManager.EnterSafeMode();

      var message = $"ARES failed to execute its closeout script. To avoid leaving lab equipment in a potentially dangerous state, safe mode has been activated.";

      await _notifier.Notify("Closeout Script Failed, Safe Mode Activated!", message, NotificationSeverityEnum.Warning);
      await _notifier.Notify("Execution Failure!", ex.Message, NotificationSeverityEnum.Error);
      _logger.LogError("Execution Failed! Safe Mode Activated! {reason}", ex.Message);
    }
  }

  public void Stop()
    => _executionControlTokenSource?.Cancel();

  public void Pause()
    => _executionControlTokenSource?.Pause();

  public void Resume()
    => _executionControlTokenSource?.Resume();

  public async Task<string> CheckCampaignStartPrerequisites()
  {
    if(_activeCampaignTemplateStore.CampaignTemplate is null)
      return "CampaignTemplate was not assigned to the active template store.";

    if(!CampaignStopConditions.Any())
      return "The Campaign has no stop conditions, please set a stop condition before starting campaign.";

    if(!EnsureParameterAssignment())
      return "The campaign has errors in it's parameter assignments, please resolve these before starting your campaign.";

    var startConditionResultTasks = _startConditions.Select(condition => condition.CanStart());
    var startConditionResults = await Task.WhenAll(startConditionResultTasks);
    startConditionResults = startConditionResults.Where(result => result is not null && !result.Success).ToArray();
    if(startConditionResults.Any())
      return $"Failed to start campaign:{Environment.NewLine}{string.Join(Environment.NewLine, startConditionResults.SelectMany(conditionResult => conditionResult!.Messages))}";

    return string.Empty;
  }

  public bool EnsureParameterAssignment()
  {
    var experimentCommandsInvalid = _activeCampaignTemplateStore.CampaignTemplate!.ExperimentTemplate.StepTemplates
    .SelectMany(step => step.CommandTemplates)
    .Any(cmd => cmd.Parameters.Any(param => param.Planned && param.PlanningMetadata is null));

    if(experimentCommandsInvalid)
      return false;

    return true;
  }

  public void UpdateReplanRate(int newRate)
  {
    ReplanRate = newRate;
  }

  private async Task PostExecution(CampaignExecutionSummary result)
  {
    await StoreCompletedCampaign(result);
    _executionControlTokenSource?.Dispose();
    _executionControlTokenSource = null;
  }

  private async Task StoreCompletedCampaign(CampaignExecutionSummary result)
  {
    try
    {
      await using var context = _dbContextFactory.CreateDbContext();
      context.CampaignExecutionSummaries.Add(result);
      await context.SaveChangesAsync();
    }

    catch(Exception)
    {
      throw;
    }

  }
}
