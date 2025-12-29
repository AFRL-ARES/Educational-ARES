using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public partial class MetadataPickerViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private Task _deviceRefreshTask = Task.CompletedTask;

  public MetadataPickerViewModel(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;
    AvailableDevices = [];
    AvailableMetadata = [];
  }

  public MetadataPickerViewModel(CommandMetadata existingMetadata, AresDevices.AresDevicesClient devicesClient) : this(devicesClient)
  {
    SelectedCommandMetadata = existingMetadata;
    AvailableDevices = [];
    AvailableMetadata = [];
  }

  [Reactive]
  public partial IEnumerable<DeviceInfo> AvailableDevices { get; private set; }

  [Reactive]
  public partial IEnumerable<CommandMetadata> AvailableMetadata { get; private set; }

  [Reactive]
  public partial CommandMetadata? SelectedCommandMetadata { get; set; }

  [Reactive]
  public partial DeviceInfo? SelectedDevice { get; set; }

  public CommandMetadata? Save()
    => SelectedCommandMetadata;

  public async Task Reset()
  {
    AvailableMetadata = Array.Empty<CommandMetadata>();
    AvailableDevices = Array.Empty<DeviceInfo>();
    await RefreshDevices();
  }

  public async Task SelectDevice(string deviceId)
  {
    var device = AvailableDevices.FirstOrDefault(d => d.UniqueId == deviceId);
    SelectedDevice = device;
    await RefreshMetadata();
  }

  public void SelectMetadata(string metadataName)
  {
    var meta = AvailableMetadata.FirstOrDefault(m => m.Name == metadataName);
    SelectedCommandMetadata = meta;
  }

  public async Task RefreshDevices()
  {
    var devicesResponse = await _devicesClient.ListAresDevicesAsync(new Empty());
    AvailableDevices = devicesResponse.AresDevices.ToArray();
    if (SelectedCommandMetadata is not null)
    {
      SelectedDevice = AvailableDevices.FirstOrDefault(d => d.UniqueId == SelectedCommandMetadata.DeviceId);
      await RefreshMetadata();
    }
  }

  public async Task RefreshMetadata()
  {
    if(SelectedDevice is null)
    {
      AvailableMetadata = Array.Empty<CommandMetadata>();
      SelectedCommandMetadata = null;
      return;
    }

    var request = new CommandMetadatasRequest { DeviceId = SelectedDevice.UniqueId };
    var metadataResponse = await _devicesClient.GetCommandMetadatasAsync(request);
    AvailableMetadata = metadataResponse.Metadatas.ToArray();

    if (SelectedCommandMetadata is null || SelectedCommandMetadata.DeviceId == SelectedDevice.UniqueId)
    {
      return;
    }

    SelectedCommandMetadata = null;
  }
}
