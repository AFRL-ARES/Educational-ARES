using Ares.Services.Device;
using AresCamera.Services;
using Google.Protobuf.WellKnownTypes;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Devices.AresCamera;

public class AresCameraMultiViewModel : UsbDeviceConnectorViewModel<AresCameraUnitControlViewModel>
{
  private readonly AresCameraRpc.AresCameraRpcClient _client;
  private INotificationReceivingService _notificationService;

  public AresCameraMultiViewModel(AresCameraRpc.AresCameraRpcClient client,
    AresDevices.AresDevicesClient devicesClient,
    INotificationReceivingService notificationService) : base(devicesClient)
  {
    _client = client;
    _notificationService = notificationService;
  }

  protected override AresCameraUnitControlViewModel CreateUnitVm(string deviceId, string deviceName) => new(deviceId, deviceName, _client, _notificationService);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var response = await _client.GetAllCamerasAsync(new Empty());
    var descriptions = response.Cameras.Select(c => new AresDeviceDescription(c.Id, c.Name)).ToArray();
    return descriptions;
  }
}
