using Ares.Services.Device;
using MK4S.Services;
using Radzen;
using Google.Protobuf.WellKnownTypes;
using UI.Backend.Repos;
using UI.Backend.ViewModels.Devices.PrusaPrinter;
using UI.Backend.ViewModels;
using DynamicData;

namespace UI.Backend.Factories;

public class PrusaDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<PrusaMK4SUnitControlViewModel>
{
  private readonly MK4SPrinterRpc.MK4SPrinterRpcClient _printerClient;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly NotificationService _notificationService;

  public PrusaDeviceControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
    MK4SPrinterRpc.MK4SPrinterRpcClient printerClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo,
    NotificationService notificationService) : base(devicesClient, deviceControlViewModelRepo)
  {
    _printerClient = printerClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _notificationService = notificationService;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
 => _deviceControlViewModelRepo.Add(new PrusaMK4SUnitControlViewModel(deviceId, deviceName, _printerClient, _notificationService));


  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var devInfos = await _printerClient.GetAllMK4SPrintersAsync(new Empty());
    var printers = devInfos.Printers.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return printers;
  }
}
