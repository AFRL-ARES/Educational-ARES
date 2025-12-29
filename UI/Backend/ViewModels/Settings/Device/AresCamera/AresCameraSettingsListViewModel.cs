using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using AresCamera;
using AresCamera.Config;
using AresCamera.Services;
using ReactiveUI;
using UI.Services.Notification;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Settings.Device.AresCamera;

public class AresCameraSettingsListViewModel : ReactiveObject
{
  private readonly AresCameraRpc.AresCameraRpcClient _client;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;

  public AresCameraSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, AresCameraRpc.AresCameraRpcClient cameraClient,
    INotificationReceivingService notificationReceivingService)
  {
    _client = cameraClient;
    _devicesClient = devicesClient;
    _notificationService = notificationReceivingService;
    _ = UpdateConfigs();
  }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new AresCameraSettingsViewModel(_client, config, _devicesClient, _notificationService, OnConfigRemoved, ConfigUpdated)).ToArray();
    SettingsViewModels = viewModels;
  }

  public AresCameraConfigEditViewModel GetNewConfigEditViewModel() => new(_client, _devicesClient);

  private async Task UpdateConfigs()
  {
    SettingsViewModels = null;
    var configs = await _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IAresCamera).FullName });

    UpdateViewModels(configs.Configs);
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  private async Task ConfigUpdated()
  {
    await UpdateConfigs();
  }

  public async Task AddNewConfig(AresCameraConfig config)
  {
    await _client.AddCameraAsync(config);
    await UpdateConfigs();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public IEnumerable<AresCameraSettingsViewModel>? SettingsViewModels { get; private set; }
}
