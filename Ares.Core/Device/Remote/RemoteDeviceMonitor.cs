using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Ares.Datamodel.Device;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote;
internal class RemoteDeviceMonitor : IDisposable
{
  private readonly RemoteDevice _device;
  private readonly Task _monitorTask;
  private readonly CancellationTokenSource _tokenSource;
  private OperationalState _lastState = OperationalState.Unspecified;
  readonly IDeviceCache _deviceCache;
  private readonly ILogger<RemoteDeviceMonitor> _logger;

  public RemoteDeviceMonitor(RemoteDevice device, IDeviceCache deviceCache, ILogger<RemoteDeviceMonitor> logger)
  {
    _deviceCache = deviceCache;
    _logger = logger;
    _device = device;
    _tokenSource = new CancellationTokenSource();
    _monitorTask = Monitor(_tokenSource.Token);
  }

  public string DeviceId => _device.UniqueId;

  public void Dispose()
  {
    _tokenSource.Cancel();
    _monitorTask.ContinueWith(_ => _tokenSource.Dispose());
  }

  private Task Monitor(CancellationToken token)
  {
    _logger.LogInformation("Started monitoring device {}", _device.Name);
    return Task.Run(async () =>
    {
      while(!token.IsCancellationRequested)
      {
        await _device.FetchOperationalStatus();

        if(_lastState == OperationalState.Active && _device.Status.OperationalState != OperationalState.Active)
        {
          _logger.LogWarning("Lost connection with device {}", _device.Name);
        }

        if(_lastState != OperationalState.Active && _device.Status.OperationalState == OperationalState.Active)
        {
          _logger.LogInformation("Device {} reconnected, fetching details", _device.Name);
          await _device.FetchInfo();
          await _device.FetchSettings();
          await _device.FetchCommands();
          await _device.FetchStateSchema();
          await _device.StopStateStream();
          _ = _device.StartStateStream();
          await _device.FetchOperationalStatus();
          await _deviceCache.CacheDeviceInfo(_device);
          await _deviceCache.CacheDeviceSettings(_device);
          _logger.LogInformation("Fetched details for {DeviceName}", _device.Name);
        }

        _lastState = _device.Status.OperationalState;

        try
        {
          var tempSource = new CancellationTokenSource();
          var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(tempSource.Token, token);
          var statusTask = _device.StatusObservable.Where(s => s.OperationalState != OperationalState.Active).ToTask(combinedSource.Token);
          var delayTask = Task.Delay(TimeSpan.FromSeconds(5), combinedSource.Token);
          _ = await Task.WhenAny(statusTask, delayTask);
          tempSource.Cancel();
        }
        catch(OperationCanceledException)
        {
        }
      }
    }, token);
  }
}
