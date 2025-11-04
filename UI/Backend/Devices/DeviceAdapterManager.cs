using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.Devices;

public class DeviceAdapterManager(
    AresDevices.AresDevicesClient _devicesClient,
    DeviceAdapterRepository _deviceAdapterRepository,
    ILoggerFactory _loggerFactory,
    ILogger<DeviceAdapterManager> _logger) : IAsyncDisposable
{
  private readonly CancellationTokenSource _cts = new();
  private Task? _pollingTask;
  private bool _isErrorState;
  private IDictionary<string, RemoteDeviceAdapterMonitor> _monitors = new Dictionary<string, RemoteDeviceAdapterMonitor>();

  public void Activate()
  {
    _pollingTask = Task.Run(() => PollDevicesAsync(_cts.Token));
  }

  private async Task PollDevicesAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        // let's stick to remote devices for now as the built-int devices have their
        // own logic in viewmodels
        var devices = await _devicesClient.ListRemoteAresDevicesAsync(
            new Empty(),
            cancellationToken: cancellationToken);

        if (_isErrorState)
        {
          _logger.LogInformation("Device polling recovered.");
          _isErrorState = false;
        }

        await UpdateAdaptersFromDeviceList(devices);
      }
      catch (OperationCanceledException)
      {
        // This is expected on shutdown.
        break;
      }
      catch (Exception ex)
      {
        if (!_isErrorState)
        {
          _logger.LogError(ex, "Error polling remote Ares devices.");
          _isErrorState = true;
        }
      }

      try
      {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
      }
      catch (OperationCanceledException)
      {
        // This is expected on shutdown.
        break;
      }
    }
  }

  private async Task UpdateAdaptersFromDeviceList(ListAresRemoteDevicesResponse devicesResponse)
  {
    try
    {
      var remoteIds = devicesResponse.Devices.Select(d => d.UniqueId).ToHashSet();
      var existingIds = _deviceAdapterRepository.Keys.ToHashSet();

      var newDevices = remoteIds.Except(existingIds);
      var removedDevices = existingIds.Except(remoteIds);

      var newAdapters = newDevices
              .Select(id => new RemoteDeviceAdapter(_devicesClient, id, _loggerFactory.CreateLogger<RemoteDeviceAdapter>())).ToArray();

      var removedAdapters = _deviceAdapterRepository.Items.Where(da => removedDevices.Contains(da.Id)).ToArray();
      foreach (var adapter in removedAdapters)
      {
        if (adapter is IAsyncDisposable asyncDisposable)
        {
          await asyncDisposable.DisposeAsync();
        }
        if (_monitors.Remove(adapter.Id, out var monitor))
        {
          monitor.Dispose();
        }
      }

      foreach (var adapter in newAdapters)
      {
        _ = adapter.Activate();
        var monitor = new RemoteDeviceAdapterMonitor(adapter, _loggerFactory.CreateLogger<RemoteDeviceAdapterMonitor>());
        _monitors[adapter.Id] = monitor;
      }

      _deviceAdapterRepository.Edit(updater =>
      {
        updater.AddOrUpdate(newAdapters); // add/update
        updater.RemoveKeys(removedDevices);   // remove stale
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating device adapter repository.");
    }
  }

  public async ValueTask DisposeAsync()
  {
    await _cts.CancelAsync();
    if (_pollingTask is not null)
    {
      await _pollingTask;
    }
    _cts.Dispose();
    GC.SuppressFinalize(this);
  }
}
