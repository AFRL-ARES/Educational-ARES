using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using MK4S.Services;
using Radzen;

namespace UI.Backend.ViewModels.Devices.PrusaPrinter;

public class PrusaMK4SMultiViewModel : UsbDeviceConnectorViewModel<PrusaMK4SUnitControlViewModel>
{
  private readonly MK4SPrinterRpc.MK4SPrinterRpcClient _client;
  private NotificationService _notificationService;

  public PrusaMK4SMultiViewModel(MK4SPrinterRpc.MK4SPrinterRpcClient client, 
    AresDevices.AresDevicesClient devicesClient, 
    NotificationService notificationService) : base(devicesClient)
  {
    _client = client;
    _notificationService = notificationService;
  }

  protected override PrusaMK4SUnitControlViewModel CreateUnitVm(string deviceId, string deviceName) => new(deviceId, deviceName, _client, _notificationService);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllMK4SPrintersAsync(new Empty());
    var descriptions = devicesResponse.Printers.Select(c => new AresDeviceDescription(c.Id, c.Name)).ToArray();
    return descriptions;
  }
}
