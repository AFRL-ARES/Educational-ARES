using Ares.Datamodel.Device;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels;

public partial class DeviceStatesViewModel : ReactiveObject
{
  readonly AresDevices.AresDevicesClient _devicesClient;

  public DeviceStatesViewModel(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;
    _devicesClient
      .ListAresDevicesAsync(new Empty()).ResponseAsync
      .ContinueWith(task => AvailableDevices = task.Result.AresDevices);
  }
  public string? SelectedDeviceName { get; set; }

  public void ChooseDevice(string deviceName)
  {

  }

  [Reactive]
  public partial IEnumerable<DeviceInfo>? AvailableDevices { get; private set; }
}
