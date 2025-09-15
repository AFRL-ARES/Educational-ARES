using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using Grpc.Core;
using MK4S.Config;
using MK4S.Services;
using ReactiveUI;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Settings.Device.PrusaMK4S;
public class PrusaMK4SSettingsViewModel : ReactiveObject
{
  private readonly MK4SPrinterRpc.MK4SPrinterRpcClient _printerClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;

  public PrusaMK4SSettingsViewModel(MK4SPrinterRpc.MK4SPrinterRpcClient printerClient,
    DeviceConfig deviceConfig,
    INotificationReceivingService notificationService,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallBack)
  {
    _printerClient = printerClient;
    _deviceConfig = deviceConfig;
    _devicesClient = devicesClient;
    _notificationService = notificationService;
    MK4SConfig = deviceConfig.ConfigData.Unpack<MK4SConfig>();
    OnRemoveCallback = onRemoveCallBack;
    EditViewModel = new PrusaMK4SConfigEditViewModel(_printerClient, _devicesClient, MK4SConfig);
  }

  public Task<DeviceOperationalStatus> GetDeviceStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = MK4SConfig.DeviceId }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Prusa MK4S Printer with a name {MK4SConfig.DeviceName}" });
    }
  }
  public async Task Save()
  {
    var printerConfig = EditViewModel.Save();
    await _printerClient.UpdateMK4SPrinterAsync(printerConfig);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceId = MK4SConfig.DeviceId
    }).ResponseAsync;

  public async Task Remove()
  {
    await _printerClient.RemoveMK4SPrinterAsync(new MK4SRequest { PrinterName = MK4SConfig.DeviceName });
    await OnRemoveCallback();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  public MK4SConfig MK4SConfig { get; }

  public Func<Task> OnRemoveCallback { get; }

  public PrusaMK4SConfigEditViewModel EditViewModel { get; }

}
