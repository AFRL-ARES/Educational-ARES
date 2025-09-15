using Ares.Services;
using AresCamera.Services;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Devices.AresCamera;

public class AresCameraUnitControlViewModel : UsbDeviceUnitViewModel
{
  private readonly AresCameraRpc.AresCameraRpcClient _client;
  private INotificationReceivingService _notificationService;

  public AresCameraUnitControlViewModel(string deviceId, string deviceName, AresCameraRpc.AresCameraRpcClient client, INotificationReceivingService notificationService) : base(deviceId, deviceName)
  {
    _client = client;
    _notificationService = notificationService;
  }

  public async Task CaptureImage()
  {
    var response = await _client.CaptureImageAsync(new CameraRequest() { CameraId = DeviceId });

    if(response.ImageData.IsEmpty)
    {
      var notification = new AresNotification();
      notification.Title = "Camera Capture Timed Out";
      notification.Message = "Camera failed to return an image within time out, no data was captured.";
      notification.NotificationSeverity = Severity.Error;
      _notificationService.PushNotification(notification);
    }

    else
    {
      ImageData = response.ImageData.ToByteArray();
    }
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  public byte[]? ImageData { get; set; }
}
