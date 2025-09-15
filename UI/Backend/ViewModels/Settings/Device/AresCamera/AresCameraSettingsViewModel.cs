using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using AresCamera.Config;
using AresCamera.Services;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Settings.Device.AresCamera;

public class AresCameraSettingsViewModel : ReactiveObject
{
  private readonly AresCameraRpc.AresCameraRpcClient _client;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;

  public AresCameraSettingsViewModel(AresCameraRpc.AresCameraRpcClient client,
    DeviceConfig deviceConfig,
    AresDevices.AresDevicesClient devicesClient,
    INotificationReceivingService notificationService,
    Func<Task> onRemoveCallBack,
    Func<Task> onUpdateCallBack)
  {
    _client = client;
    _deviceConfig = deviceConfig;
    _devicesClient = devicesClient;
    _notificationService = notificationService;
    AresCameraConfig = deviceConfig.ConfigData.Unpack<AresCameraConfig>();
    SelectedSource = AresCameraConfig.SourceName;
    OnRemoveCallback = onRemoveCallBack;
    OnUpdateCallback = onUpdateCallBack;
    EditViewModel = new AresCameraConfigEditViewModel(_client, _devicesClient, AresCameraConfig);
  }

  public Task<DeviceOperationalStatus> GetDeviceStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = AresCameraConfig.DeviceId }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Camera with a name {AresCameraConfig.DeviceName}" });
    }
  }

  public async Task Init()
  {
    var status = await GetDeviceStatus();
    if(status.OperationalState is not OperationalState.Active)
      throw new InvalidOperationException();

    var response = await _client.UpdateAvailableSourcesAsync(new CameraRequest() { CameraId = AresCameraConfig.DeviceId });
    AvailableSources = response.AvailableSources.ToList();
  }

  public async Task Save()
  {
    var aresCameraConfig = EditViewModel.Save();
    await _client.UpdateCameraAsync(aresCameraConfig);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceId = AresCameraConfig.DeviceId
    }).ResponseAsync;

  public async Task Remove()
  {
    await _client.RemoveCameraAsync(new CameraRequest() { CameraId = AresCameraConfig.DeviceId });
    await OnRemoveCallback();
  }

  public async Task SwitchSource(object args)
  {
    if(args is not string source)
      return;

    AresCameraConfig.SourceName = source;
    await _client.UpdateCameraSourceAsync(AresCameraConfig);
    await _client.UpdateCameraAsync(AresCameraConfig);
    await OnUpdateCallback();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);
  public AresCameraConfig AresCameraConfig { get; }

  public Func<Task> OnRemoveCallback { get; }

  public Func<Task> OnUpdateCallback { get; }

  public AresCameraConfigEditViewModel EditViewModel { get; }

  [Reactive]
  public List<string> AvailableSources { get; set; } = new List<string>();

  [Reactive]
  public string? SelectedSource { get; set; }
}
