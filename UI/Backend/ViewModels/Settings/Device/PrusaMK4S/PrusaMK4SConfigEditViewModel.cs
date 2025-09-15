using Ares.Services.Device;
using MK4S.Config;
using MK4S.Services;
using ReactiveUI;
using System.ComponentModel.DataAnnotations;

namespace UI.Backend.ViewModels.Settings.Device.PrusaMK4S;

public class PrusaMK4SConfigEditViewModel : ReactiveObject
{
  private readonly MK4SPrinterRpc.MK4SPrinterRpcClient _client;
  private readonly MK4SConfig _config;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public PrusaMK4SConfigEditViewModel(MK4SPrinterRpc.MK4SPrinterRpcClient client,
    AresDevices.AresDevicesClient devicesClient)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = new MK4SConfig();
    NewConfig = true;
  }

  public PrusaMK4SConfigEditViewModel(MK4SPrinterRpc.MK4SPrinterRpcClient client,
    AresDevices.AresDevicesClient devicesClient,
    MK4SConfig config)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = config;
    _name = config.DeviceName;
    Username = config.Username;
    Password = config.Password;
    Address = config.Address;
    Simulated = config.Simulated;
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
  public bool Simulated { get; set; }
  public string? Username { get; set; }
  public string? Password { get; set; }
  public string? Address { get; set; }
  public bool Modified
  => _config.DeviceName != Name
  || _config.Address != Address
  || _config.Username != Username
  || _config.Password != Password
  || _config.Simulated != Simulated;

  public MK4SConfig Save()
    => Modified ? new MK4SConfig
    {
      DeviceName = Name,
      Address = Address,
      Username = Username,
      Password = Password,
      Simulated = Simulated
    } : _config;
}
