using Ares.Datamodel.Device;
using Ares.Services.Device;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UI.Backend.Repos;
using UI.Backend.ViewModels;

namespace UI.Backend.Factories;

public abstract class DeviceConnectorViewModelFactory<TDeviceUnitVm> : ReactiveObject, IAsyncDisposable where TDeviceUnitVm : DeviceUnitControlViewModel
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private IDisposable _deviceUpdater = Disposable.Empty;

  public DeviceConnectorViewModelFactory(AresDevices.AresDevicesClient devicesClient, IDeviceControlViewModelRepo deviceControlViewModelRepo)
  {
    _devicesClient = devicesClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected abstract Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices();

  public void Start(TimeSpan interval)
  {
    _deviceUpdater = Observable
      .Interval(interval)
      .StartWith(0)
      .SelectMany(async _ =>
      {
        try
        {
          await UpdateAvailableDevices();
        }

        catch (Exception ex)
        {
          //TODO: Log this info
          Console.WriteLine($"Error updating devices: {ex.Message}");
        }

        return Unit.Default;
      })
      .Subscribe();
  }

  private async Task UpdateAvailableDevices()
  {
    var deviceDescriptions = await GetAvailableDevices();
    foreach(var description in deviceDescriptions)
    {
      var deviceStatusRequest = new DeviceStatusRequest { DeviceId = description.Id };
      var deviceStatusResponse = _devicesClient.GetDeviceStatus(deviceStatusRequest);

      if(deviceStatusResponse.OperationalState == OperationalState.Active)
      {
        if(_deviceControlViewModelRepo.Items.Any(vm => vm.DeviceId.Equals(description.Id)))
          continue;

        CreateAndAddViewModel(description.Id, description.Name);
        continue;
      }
    }
  }

  protected abstract void CreateAndAddViewModel(string deviceId, string deviceName);

  public async ValueTask DisposeAsync()
  {
    _deviceUpdater.Dispose();
    var vms = ConnectedDeviceUnitControlVms.OfType<IAsyncDisposable>();
    foreach(var vm in vms)
    {
      await vm.DisposeAsync();
    }
  }

  public ReadOnlyObservableCollection<TDeviceUnitVm> ConnectedDeviceUnitControlVms { get; }

}