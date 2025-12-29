using UI.Backend.Devices;
using UI.Backend.Factories;
using UI.Backend.Repos;
using UI.Services.Notification;

namespace UI;

public class ServiceStarter : IHostedService
{
  private readonly INotificationReceivingService _notificationReceivingService;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly DeviceAdapterManager _deviceAdapterManager;
  private readonly AresCameraControlViewModelFactory _cameraViewModelFactory;
  private readonly PrusaDeviceControlViewModelFactory _prusaViewModelFactory;

  public ServiceStarter(
    INotificationReceivingService notificationReceivingService,
    IServiceProvider serviceProvider,
    IDeviceControlViewModelRepo deviceControlViewModelRepo,
    DeviceAdapterManager deviceAdapterManager,
    AresCameraControlViewModelFactory cameraViewModelFactory,
    PrusaDeviceControlViewModelFactory prusaViewModelFactory)
  {
    _notificationReceivingService = notificationReceivingService;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _deviceAdapterManager = deviceAdapterManager;
    _cameraViewModelFactory = cameraViewModelFactory;
    _prusaViewModelFactory = prusaViewModelFactory;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _notificationReceivingService.StartNotificationStream();
    _deviceControlViewModelRepo.Initialize();
    _deviceAdapterManager.Activate();
    _cameraViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _prusaViewModelFactory.Start(TimeSpan.FromSeconds(5));
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _deviceControlViewModelRepo.Dispose();
    await _deviceAdapterManager.DisposeAsync();
  }
}