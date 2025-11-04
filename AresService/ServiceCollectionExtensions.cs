using System.IO;
using Ares.Core;
using Ares.Core.Device;
using Ares.Core.Grpc;
using Ares.Core.Execution;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using AresService.DeviceManagers;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Core.Device.State.Logging;
using AresService.ConfigManagers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AresCamera;
using AresCamera.Config;
using MK4S.Config;
using PrusaMK4S;
using Serilog;

namespace AresService;

public static class ServiceCollectionExtensions
{
  public static void AddAres(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSingleton<AresStarter>();
    services.AddSingleton<ISerialConnectionRepository, SerialConnectionRepository>();

    services.AddDeviceManagers();
    services.AddDeviceStateLoggers();
    services.AddAresCoreComponents();
    services.AddNotificationHandlers();
    services.BindStateExporters();

    services.RemoveAll<IDeviceCommandInterpreterRepo>();
    services.AddSingleton<IDeviceCommandInterpreterRepo, DeviceCommandInterpreterRepo>();


    services.AddSingleton<IExecutionSummaryHandler>(provider =>
      {
        var stateExporters = provider.GetServices<IDeviceStateExportStreamProvider>();
        return new ExperimentResultJsonHandler(stateExporters);
      });
  }

  private static void AddDeviceManagers(this IServiceCollection services)
  {
    //Database Loaders
    services.AddTransient<IDeviceDbLoader, PrusaMK4SPrinterDbLoader>();
    services.AddTransient<IDeviceDbLoader, CameraDbLoader>();

    //Config Managers
    services.AddTransient<IDeviceConfigManager<MK4SConfig>, MK4SPrinterConfigManager>();
    services.AddTransient<IDeviceConfigManager<AresCameraConfig>, AresCameraConfigManager>();

    //Device Managers
    services.AddTransient<IDeviceManager<MK4SConfig, IPrusaMK4S>, PrusaMK4SPrinterDeviceManager>();
    services.AddTransient<IDeviceManager<AresCameraConfig, IAresCamera>, AresCameraDeviceManager>();
  }

  private static void BindStateExporters(this IServiceCollection services)
  {
    services.AddSingleton<IDeviceStateExportStreamProvider, CombinedDeviceStateExportStreamProvider>();
    services.AddSingleton<IDeviceStateExportStreamProvider, ZippedStatesExportStreamProvider>();
    services.AddSingleton<IDeviceStateGetter, DeviceStateGetter>();
  }

  private static void AddDeviceStateLoggers(this IServiceCollection services)
  {
    services.AddSingleton<IDeviceStateLoggerRepository, DeviceStateLoggerRepository>();
    services.AddSingleton<IDeviceStateGetter, DeviceStateGetter>();
  }
}
