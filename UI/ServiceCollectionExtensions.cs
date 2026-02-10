using Ares.Services;
using Ares.Services.Device;
using AresCamera.Services;
using Grpc.Health.V1;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MK4S.Services;
using Radzen;
using UI.Areas.Identity;
using UI.Backend.Devices;
using UI.Backend.Factories;
using UI.Backend.Helpers;
using UI.Backend.Notifications;
using UI.Backend.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.Automation;
using UI.Backend.ViewModels.Automation.CampaignEdit;
using UI.Backend.ViewModels.Automation.CampaignEdit.Factories;
using UI.Backend.ViewModels.Automation.Planning;
using UI.Backend.ViewModels.DeviceStateLogging;
using UI.Backend.ViewModels.Factories;
using UI.Backend.ViewModels.Misc;
using UI.Backend.ViewModels.Settings.Analysis;
using UI.Backend.ViewModels.Settings.Device.AresCamera;
using UI.Backend.ViewModels.Settings.Device.PrusaMK4S;
using UI.Backend.ViewModels.Settings.Logging;
using UI.Backend.ViewModels.Settings.Planning;
using UI.Services.CampaignEdit;
using UI.Services.Grpc;
using UI.Services.ServerHealth;
using UI.Services.ServerHealthNotification;

namespace UI;

internal static class ServiceCollectionExtensions
{
  public static void LoadAresModules(this IServiceCollection services)
  {
    services.AddScoped<ServerHealthService>();
    services.AddScoped<ServerHealthNotificationService>();
    services.AddScoped<DialogService>();
    services.AddSingleton<NotificationService>();
    services.AddScoped<TooltipService>();
    services.AddScoped<ContextMenuService>();
    services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
    services.AddSingleton<UnitCategoryHelper>();
    services.AddScoped<CampaignEditContext>();
    services.BindViewModels();
    services.BindViewModelFactories();
    services.AddScoped<ICombinedDeviceGetter, CombinedDeviceGetter>();
    services.AddSingleton<IDeviceControlViewModelRepo, DeviceControlViewModelRepo>();
    services.AddSingleton<INotificationRepository, NotificationRepository>();

    services.AddSingleton<DeviceAdapterRepository>();
    services.AddSingleton<DeviceAdapterManager>();
  }

  public static void BindClients(this IServiceCollection services)
  {
    var tempProvider = services.BuildServiceProvider();
    var clientManager = tempProvider.GetRequiredService<IClientManager>();

    //Ares Clients
    services.AddScoped(_ => clientManager.GetClient<AresServerInfo.AresServerInfoClient>());
    services.AddScoped(_ => clientManager.GetClient<AresAutomation.AresAutomationClient>());
    services.AddScoped(_ => clientManager.GetClient<Health.HealthClient>());
    services.AddScoped(_ => clientManager.GetClient<AresPlannerManagementService.AresPlannerManagementServiceClient>());
    services.AddScoped(_ => clientManager.GetClient<AresValidation.AresValidationClient>());
    services.AddScoped(_ => clientManager.GetClient<AresAnalyzerManagementService.AresAnalyzerManagementServiceClient>());
    services.AddScoped(_ => clientManager.GetClient<AresAnalysisService.AresAnalysisServiceClient>());
    services.AddScoped(_ => clientManager.GetClient<AresSafetyService.AresSafetyServiceClient>());
    services.AddSingleton(_ => clientManager.GetClient<AresNotificationRpc.AresNotificationRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<AresScriptingService.AresScriptingServiceClient>());

    //Device Clients
    services.AddSingleton(_ => clientManager.GetClient<AresDevices.AresDevicesClient>());
    services.AddSingleton(_ => clientManager.GetClient<MK4SPrinterRpc.MK4SPrinterRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<AresCameraRpc.AresCameraRpcClient>());

    //Device State Logging Clients
    services.AddScoped(_ => clientManager.GetClient<DeviceStateExportService.DeviceStateExportServiceClient>());
  }

  private static void BindViewModels(this IServiceCollection services)
  {
    services.AddScoped<DataViewerViewModel>();
    services.AddScoped<IndexViewModel>();
    services.AddScoped<NotificationHistoryViewModel>();
    services.AddScoped<ProfileViewModel>();
    services.AddScoped<ProjectViewModel>();
    services.AddScoped<QuasiManualViewModel>();
    services.AddScoped<SettingsViewModel>();
    services.AddTransient<CampaignDesignerViewModel>();
    services.AddScoped<CampaignListViewModel>();
    services.AddScoped<CustomStepBuilderViewModel>();
    services.AddScoped<ExecutionHistoryViewModel>();
    services.AddScoped<ExecutionViewModel>();
    services.AddScoped<ScriptPlaygroundViewModel>();

    //Device Settings List View Models
    services.AddTransient<DeviceStatesViewModel>();
    services.AddTransient<DeviceStateExporterViewModel>();
    services.AddTransient<LoggingSettingsListViewModel>();
    services.AddTransient<AnalyzerSettingsListViewModel>();
    services.AddTransient<PlannerSettingsListViewModel>();
    services.AddScoped<PrusaMK4SSettingsListViewModel>();
    services.AddScoped<AresCameraSettingsListViewModel>();

    //Other View Models
    services.AddTransient<DeviceStatesViewModel>();
    services.AddTransient<DeviceStateExporterViewModel>();
    services.AddScoped<ManualPlannerViewModel>();
    services.AddScoped<ManualExecutionWidgetViewModel>();
    services.AddScoped<LoggingSettingsListViewModel>();
  }
  private static void BindViewModelFactories(this IServiceCollection services)
  {
    services.AddScoped<CommandDesignerFactory>();
    services.AddScoped<CommandParameterDesignerFactory>();
    services.AddScoped<ExperimentDesignerFactory>();
    services.AddScoped<StartupDesignerFactory>();
    services.AddScoped<CloseoutDesignerFactory>();
    services.AddScoped<MetadataPickerFactory>();
    services.AddScoped<ParameterEditorFactory>();
    services.AddScoped<PlannableParameterDesignerFactory>();
    services.AddScoped<StepDesignerFactory>();
    services.AddScoped<PlanningDesignerFactory>();
    services.AddScoped<AnalyzerInputDesignerVmFactory>();
    services.AddScoped<DeviceStateFilterViewModelFactory>();
    services.AddSingleton<AresCameraControlViewModelFactory>();
    services.AddSingleton<PrusaDeviceControlViewModelFactory>();
  }
}
