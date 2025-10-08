using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Device;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Core.Tests.Data;
using Ares.Core.Tests.Data.Analyzer;
using Ares.Core.Tests.Data.Device;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Tests.Execution;

internal class CampaignExecutorTests
{
  private IAnalyzerRepo _analyzerRepo;
  private CampaignComposer _campaignComposer;
  private ICampaignExecutor _campaignExecutor;
  private IExecutionReporter _executionReporter;
  private IExecutionReportStore _executionReportStore;
  private IPlanningHelper _planningHelper;
  private IEnumerable<IExecutionSummaryHandler> _resultHandlers;
  private AresVariableManager _variableManager;
  private AnalysisHelper _analysisHelper;
  private AnalysisRepo _analysisRepo;
  private StateLoggerManager _stateLoggerManager;
  private IEnumerable<INotificationHandler> _notificationHandlers;

  private IAnalyzer _replyAnalyzer;

  [OneTimeSetUp]
  public void OneTimeSetUp()
  {
    _analyzerRepo = new AnalyzerRepo();
    _replyAnalyzer = new TestReplyAnalyzer();
    _analyzerRepo.AddAnalyzer(_replyAnalyzer);
    _analysisRepo = new AnalysisRepo();
    _analysisHelper = new AnalysisHelper(_analyzerRepo);
    _executionReportStore = new ExecutionReportStore();
    _executionReporter = new ExecutionReporter(_executionReportStore);
    _planningHelper = new Mock<IPlanningHelper>().Object;
    _resultHandlers = new Mock<List<IExecutionSummaryHandler>>().Object;
    _variableManager = new Mock<AresVariableManager>().Object;
    _notificationHandlers = new Mock<IEnumerable<INotificationHandler>>().Object;

    var device = new TestDevice();
    var cmdInterpreter = new TestDeviceInterpreter(device);
    var repo = new DeviceCommandInterpreterRepo()
    {
      cmdInterpreter
    };
    var stepComposer = new StepComposer(repo, _notificationHandlers);
    var experimentComposer = new ExperimentComposer(stepComposer, _analyzerRepo);

    var stateLoggerRepository = new DeviceStateLoggerRepository();
    var factories = Array.Empty<IDeviceStateLoggerFactory>();
    var logger = Mock.Of<ILogger<StateLoggerManager>>();
    var dbContextFactory = Mock.Of<IDbContextFactory<CoreDatabaseContext>>();
    _stateLoggerManager = new StateLoggerManager(stateLoggerRepository, factories, logger, dbContextFactory);

    _campaignComposer = new CampaignComposer(_analysisHelper, experimentComposer, _planningHelper, _executionReporter, _resultHandlers, _analysisRepo, _analyzerRepo, Array.Empty<INotificationHandler>(), _variableManager, _stateLoggerManager);
  }

  [SetUp]
  public void SetUp()
  {
    _campaignExecutor = _campaignComposer.Compose(TestCampaignProvider.GetSampleCampaignTemplate(_replyAnalyzer));
  }

  [Test]
  public void Executor_Should_Execute_Valid_Template_Without_Exception()
  {
    var controlTokenSource = new ExecutionControlTokenSource();
    var stopCondition = new NumExperimentsRun(_executionReportStore, 1);
    _campaignExecutor.StopConditions.Add(stopCondition);
    Assert.DoesNotThrowAsync(() => _campaignExecutor.Execute(controlTokenSource.Token));
  }
}
