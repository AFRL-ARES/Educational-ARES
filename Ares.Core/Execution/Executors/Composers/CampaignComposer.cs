using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Device.State.Logging;
using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class CampaignComposer : ICommandComposer<CampaignTemplate, ICampaignExecutor>
{
  private readonly IExecutionReporter _executionReporter;
  private readonly ICommandComposer<ExperimentTemplate, ExperimentExecutor> _experimentComposer;
  private readonly IPlanningHelper _planningHelper;
  private readonly IEnumerable<IExecutionSummaryHandler> _resultHandlers;
  private readonly IEnumerable<INotificationHandler> _notificationHandlers;
  private readonly AresVariableManager _variableManager;
  private readonly StateLoggerManager _stateLoggerManager;
  readonly AnalysisHelper _analysisHelper;
  readonly AnalysisRepo _analysisRepo;
  readonly IAnalyzerRepo _analyzerRepo;

  public CampaignComposer(AnalysisHelper analysisHelper,
    ICommandComposer<ExperimentTemplate, ExperimentExecutor> experimentComposer,
    IPlanningHelper planningHelper,
    IExecutionReporter executionReporter,
    IEnumerable<IExecutionSummaryHandler> resultHandlers,
    AnalysisRepo analysisRepo,
    IAnalyzerRepo analyzerRepo,
    IEnumerable<INotificationHandler> notificationHandlers,
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
    _resultHandlers = resultHandlers;
    _notificationHandlers = notificationHandlers;
  }

  public ICampaignExecutor Compose(CampaignTemplate template)
    => new CampaignExecutor(_experimentComposer, _planningHelper, _executionReporter, _analysisHelper, template, _resultHandlers, _analysisRepo, _notificationHandlers, _analyzerRepo, _variableManager, _stateLoggerManager);
}
