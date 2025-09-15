using Ares.Services.Device;
using AresCamera.Config;
using AresCamera.Services;
using ReactiveUI;
using System.ComponentModel.DataAnnotations;

namespace UI.Backend.ViewModels.Settings.Device.AresCamera;

public class AresCameraConfigEditViewModel : ReactiveObject
{
  private readonly AresCameraRpc.AresCameraRpcClient _client;
  private readonly AresCameraConfig _AresCameraConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public AresCameraConfigEditViewModel(AresCameraRpc.AresCameraRpcClient client,
    AresDevices.AresDevicesClient devicesClient)
  {
    _client = client;
    _devicesClient = devicesClient;
    _AresCameraConfig = new AresCameraConfig();
    NewConfig = true;
  }

  public AresCameraConfigEditViewModel(AresCameraRpc.AresCameraRpcClient client,
    AresDevices.AresDevicesClient devicesClient,
    AresCameraConfig config)
  {
    _client = client;
    _devicesClient = devicesClient;
    _AresCameraConfig = config;
    NewConfig = false;
  }

  [Required]
  public string? Name
  {
    get => _name;

    set
    {
      if(!NewConfig)
        return;

      _name = value;
    }
  }

  public bool NewConfig { get; set; }

  public bool Modified { get; set; }

  public AresCameraConfig Save()
  {
    var config = Modified ? new AresCameraConfig() { DeviceName = Name } : _AresCameraConfig;

    config.DeviceName = Name;

    return config;
  }

}
