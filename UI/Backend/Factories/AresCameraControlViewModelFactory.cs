using Ares.Services.Device;
using AresCamera.Services;
using UI.Backend.Repos;
using UI.Backend.ViewModels;
using Google.Protobuf.WellKnownTypes;
using UI.Backend.ViewModels.Devices.AresCamera;
using UI.Services.Notification;
using DynamicData;

namespace UI.Backend.Factories;

public class AresCameraControlViewModelFactory : DeviceConnectorViewModelFactory<AresCameraUnitControlViewModel>
{
  private readonly AresCameraRpc.AresCameraRpcClient _cameraClient;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly INotificationReceivingService _notificationService;

  public AresCameraControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
    AresCameraRpc.AresCameraRpcClient cameraClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo,
    INotificationReceivingService notificationService) : base(devicesClient, deviceControlViewModelRepo)
  {
    _cameraClient = cameraClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _notificationService = notificationService;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
    => _deviceControlViewModelRepo.Add(new AresCameraUnitControlViewModel(deviceId, deviceName, _cameraClient, _notificationService));


  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var devInfos = await _cameraClient.GetAllCamerasAsync(new Empty());
    var cams = devInfos.Cameras.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return cams;
  }
}
