using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using DynamicData;
using ReactiveUI;

namespace UI.Backend.ViewModels;

public abstract class SerialDeviceConnectorViewModel<TDeviceUnitVm> : ReactiveObject, IAsyncDisposable where TDeviceUnitVm : SerialDeviceUnitViewModel
{
  private readonly ISourceCache<TDeviceUnitVm, string> _connectedSerialDeviceUnitControlVmsSource =
  new SourceCache<TDeviceUnitVm, string>(vm => vm.DeviceId);
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private IDisposable _deviceUpdater = Disposable.Empty;
  private CancellationTokenSource _deviceUpdaterTokenSource = new CancellationTokenSource();

  public SerialDeviceConnectorViewModel(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;

    _connectedSerialDeviceUnitControlVmsSource.Connect().Bind(out var connectedSerialDeviceUnitControlVms).Subscribe();
    ConnectedSerialDeviceUnitControlVms = connectedSerialDeviceUnitControlVms;
  }

  protected abstract Task<AresDeviceDescription[]> GetDeviceDescriptions();

  public void Start(TimeSpan interval)
  {
    _deviceUpdater = Task.Factory.StartNew(async () =>
    {
      while(!_deviceUpdaterTokenSource.Token.IsCancellationRequested)
      {
        await UpdateAvailableDevices();
        await Task.Delay(interval, _deviceUpdaterTokenSource.Token);
      }
    }, _deviceUpdaterTokenSource.Token);
  }

  private async Task UpdateAvailableDevices()
  {
    var descriptions = await GetDeviceDescriptions();
    foreach(var description in descriptions)
    {
      var deviceStatusRequest = new DeviceStatusRequest { DeviceId = description.Id };
      var deviceOperationalStatusResponse = await _devicesClient.GetDeviceStatusAsync(deviceStatusRequest);

      if(deviceOperationalStatusResponse.OperationalState == OperationalState.Active)
      {
        if(ConnectedSerialDeviceUnitControlVms.Any(vm => vm.DeviceId == description.Id))
          continue;

        var unitVm = CreateUnitVm(description);
        _connectedSerialDeviceUnitControlVmsSource.AddOrUpdate(unitVm);
        continue;
      }

      if(deviceOperationalStatusResponse.OperationalState == OperationalState.Inactive)
      {
        if(ConnectedSerialDeviceUnitControlVms.Any(vm => vm.DeviceId == description.Id))
          _connectedSerialDeviceUnitControlVmsSource.Remove(description.Id);

        continue;
      }

      if(deviceOperationalStatusResponse.OperationalState == OperationalState.Error)
        if(ConnectedSerialDeviceUnitControlVms.Any(vm => vm.DeviceId == description.Id))
          _connectedSerialDeviceUnitControlVmsSource.Remove(description.Id);
    }
  }

  protected abstract TDeviceUnitVm CreateUnitVm(AresDeviceDescription description);

  protected AresDevices.AresDevicesClient DevicesClient => _devicesClient;
  
  public async ValueTask DisposeAsync()
  {
    _deviceUpdaterTokenSource.Cancel();
    _deviceUpdater.Dispose();
    var vms = ConnectedSerialDeviceUnitControlVms.OfType<IAsyncDisposable>();
    foreach(var vm in vms)
    {
      await vm.DisposeAsync();
    }
  }

  public string[]? DiscoveredSerialPorts { get; set; }

  public string? SelectedSerialPort { get; set; }

  public string[]? DiscoveredDeviceNames { get; set; }

  //public string? SelectedDeviceName { get; set; }

  public string? SelectedDeviceId { get; set; }
  public ReadOnlyObservableCollection<TDeviceUnitVm> ConnectedSerialDeviceUnitControlVms { get; }

}