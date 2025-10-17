using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Device;
using Ares.Core.Device.Helpers;
using Ares.Core.Device.Remote;
using Ares.Core.Device.Remote.State;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Safety;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Core.Validation.Campaign;
using Ares.Datamodel.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace Ares.Core;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Binds all the necessary ARES core components into an <see cref="IServiceCollection" />
  /// </summary>
  /// <param name="services"></param>
  public static void AddAresCoreComponents(this IServiceCollection services)
  {
    services.AddSingleton<IExecutionManager, ExecutionManager>();
    services.AddSingleton<IExecutionSafetyManager, ExecutionSafetyManager>();
    services.AddSingleton<IPlanningHelper, PlanningHelper>();
    services.AddSingleton<IExecutionReporter, ExecutionReporter>();
    services.AddSingleton<IExecutionReportStore, ExecutionReportStore>();
    services.AddSingleton<IAnalyzerRepo, AnalyzerRepo>();
    services.AddSingleton<IPlannerServiceRepo, PlannerServiceRepo>();
    services.AddTransient<INumExperimentsRunFactory, NumExperimentsRunFactory>();
    services.AddSingleton<IActiveCampaignTemplateStore, ActiveCampaignTemplateStore>();
    services.AddSingleton<ICampaignValidatorRepository, CampaignValidatorRepository>();
    services.AddTransient<ICampaignValidator, AllPlannersAssignedCampaignValidator>();
    services.AddTransient<ICampaignValidator, GoodAnalyzerCampaignValidator>();
    services.AddTransient<ICampaignValidator, RequiredDeviceInterpretersValidator>();
    services.AddSingleton<IDeviceCommandInterpreterRepo, DeviceCommandInterpreterRepo>();
    services.AddSingleton<IRemoteAnalyzerManager, RemoteAnalyzerManager>();
    services.AddSingleton<IRemotePlannerManager, RemotePlannerManager>();
    services.AddSingleton<IAnalyzerCache, AnalyzerCache>();
    services.AddSingleton<IRemoteDeviceManager, RemoteDeviceManager>();
    services.AddSingleton<IDeviceCache, DeviceCache>();
    services.AddSingleton<IPlannerServiceCache, PlannerServiceCache>();
    services.AddSingleton<AresVariableManager>();
    services.AddSingleton<AnalysisRepo>();
    services.AddSingleton<PlannerServiceRepo>();
    services.AddSingleton<AnalysisHelper>();
    services.AddSingleton<IDesiredAnalysisResultFactory, DesiredAnalysisResultFactory>();
    services.AddSingleton<DeviceIdHelper>();
    services.AddSingleton<INotifier, Notifier>();

    services.BindComposers();
    services.BindStartConditions();
    services.BindStateLogging();
  }

  private static void BindStateLogging(this IServiceCollection services)
  {
    services.AddSingleton<StateLoggerManager>();
    services.AddSingleton<IDeviceStateStreamProvider, DeviceStateStreamProvider>();
    services.AddSingleton<IDeviceStateDataProvider, RemoteDeviceExportDataProvider>();
    services.AddSingleton<IDeviceStateExportStreamProvider, CombinedDeviceStateExportStreamProvider>();
    services.AddSingleton<IDeviceStateExportStreamProvider, ZippedStatesExportStreamProvider>();
    services.AddSingleton<IDeviceStateLoggerRepository, DeviceStateLoggerRepository>();
    services.AddSingleton<IDeviceStateGetter, DeviceStateGetter>();
    services.AddSingleton<IDeviceStateLoggerFactory, RemoteDeviceStateLoggerFactory>();
  }

  private static void BindStartConditions(this IServiceCollection services)
  {
    services.AddTransient<IStartCondition, CampaignInProgressStartCondition>();
    services.AddTransient<IStartCondition, AllPlannersAssignedStartCondition>();
    services.AddTransient<IStartCondition, ValidPlannerParamTypeStartCondition>();
    services.AddTransient<IStartCondition, GoodAnalyzerForExperimentOutputCondition>();
    services.AddTransient<IStartCondition, RequiredDeviceInterpretersStartCondition>();
    services.AddTransient<IStartCondition, AssignedPlannersActiveStartCondition>();
  }

  private static void BindComposers(this IServiceCollection services)
  {
    services.AddSingleton<ICommandComposer<StepTemplate, StepExecutor>, StepComposer>();
    services.AddSingleton<ICommandComposer<ExperimentTemplate, ExperimentExecutor>, ExperimentComposer>();
    services.AddSingleton<ICommandComposer<CampaignTemplate, ICampaignExecutor>, CampaignComposer>();
  }
}
