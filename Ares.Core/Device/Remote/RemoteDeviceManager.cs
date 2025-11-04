using Ares.Core.Device.State.Logging;
using Ares.Core.Notifications;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote;
internal class RemoteDeviceManager(
  IDeviceCommandInterpreterRepo _deviceCommandInterpreters,
  IDeviceCache _deviceCache,
  INotificationHandler _notificationHandler,
  IDbContextFactory<CoreDatabaseContext> _dbContextFactory,
  StateLoggerManager _stateLoggerManager,
  ILoggerFactory _loggerFactory,
  ILogger<RemoteDeviceManager> _logger) : IRemoteDeviceManager
{
  private readonly List<RemoteDeviceMonitor> _deviceMonitors = [];

  public async Task<RemoteDevice> CreateDevice(string name, string url)
  {
    var config = new RemoteDeviceConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var device = ConfigToDevice(config);

    _deviceCommandInterpreters.Add(new RemoteDeviceCommandInterpreter(device));
    var monitor = new RemoteDeviceMonitor(device, _deviceCache, _loggerFactory.CreateLogger<RemoteDeviceMonitor>());
    _deviceMonitors.Add(monitor);

    var ctx = _dbContextFactory.CreateDbContext();
    ctx.RemoteDeviceConfigs.Add(config);

    await device.Activate(CancellationToken.None);

    await _stateLoggerManager.SetupLogger(device);

    await ctx.SaveChangesAsync();
    return device;
  }

  private RemoteDevice ConfigToDevice(RemoteDeviceConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _logger.LogError("Failed to load a remote device {DeviceName} because the url {DeviceUrl} is invalid.", config.Name, config.Url);
      _ = _notificationHandler.HandleNotification(
        "Device Load Error",
        $"Failed to load a remote device {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Danger);
      throw new InvalidOperationException($"Failed to load a remote device {config.Name} because the url {config.Url} is invalid.");
    }

    var device = new RemoteDevice(config.Name, uri, config.UniqueId);

    return device;
  }

  public async Task LoadDevices()
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var configs = await ctx.RemoteDeviceConfigs.ToArrayAsync();
    var devices = await Task.WhenAll(configs.Select(LoadExistingDevice));
    var nonNullDevices = devices.OfType<RemoteDevice>().ToArray();
    foreach(var device in nonNullDevices)
    {
      _deviceCommandInterpreters.Add(new RemoteDeviceCommandInterpreter(device));

      var monitor = new RemoteDeviceMonitor(device, _deviceCache, _loggerFactory.CreateLogger<RemoteDeviceMonitor>());
      _deviceMonitors.Add(monitor);

      await _stateLoggerManager.SetupLogger(device);
    }
  }

  public async Task<bool> RemoveDevice(string deviceId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var device = ctx.RemoteDeviceConfigs.Where(a => a.UniqueId == deviceId).FirstOrDefault();
    if(device is null)
    {
      return false;
    }

    ctx.Remove(device);
    await ctx.SaveChangesAsync();

    _deviceCommandInterpreters.Remove(deviceId);
    var monitor = _deviceMonitors.First(m => m.DeviceId == deviceId);
    monitor.Dispose();
    _deviceMonitors.Remove(monitor);

    await _stateLoggerManager.RemoveLogger(device.UniqueId);

    return true;
  }

  public async Task UpdateDevice(RemoteDeviceConfig config)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var deviceCfg = ctx.RemoteDeviceConfigs.Where(a => a.UniqueId == config.UniqueId).FirstOrDefault();
    if(deviceCfg is null)
    {
      return;
    }

    deviceCfg.Name = config.Name;
    deviceCfg.Url = config.Url;
    await ctx.SaveChangesAsync();

    _deviceCommandInterpreters.Remove(deviceCfg.UniqueId);
    var monitor = _deviceMonitors.First(m => m.DeviceId == deviceCfg.UniqueId);
    monitor.Dispose();
    _deviceMonitors.Remove(monitor);
    await _stateLoggerManager.RemoveLogger(deviceCfg.UniqueId);

    var device = await LoadExistingDevice(deviceCfg);
    if(device is null)
    {
      return;
    }

    await _stateLoggerManager.SetupLogger(device);

    monitor = new RemoteDeviceMonitor(device, _deviceCache, _loggerFactory.CreateLogger<RemoteDeviceMonitor>());
    _deviceMonitors.Add(monitor);
    _deviceCommandInterpreters.Add(new RemoteDeviceCommandInterpreter(device));


  }

  public Task UpdateDeviceSettings(DeviceSettings deviceSettings)
  {
    var aresDevice = _deviceCommandInterpreters.FirstOrDefault(dci => dci.Device.UniqueId == deviceSettings.DeviceId)?.Device;
    if(aresDevice is not RemoteDevice device)
    {
      return Task.CompletedTask;
    }

    device.UpdateSettings(deviceSettings.Settings);

    if(device is not RemoteDevice remoteDevice)
      return Task.CompletedTask;

    return _deviceCache.CacheDeviceSettings(remoteDevice);
  }

  private async Task<RemoteDevice?> LoadExistingDevice(RemoteDeviceConfig config)
  {
    var device = ConfigToDevice(config);
    if(device is null)
      return null;

    var deviceInfo = await _deviceCache.GetCachedDeviceInfo(config.UniqueId);
    if(deviceInfo is not null)
    {
      await device.UpdateInfo(deviceInfo);
    }

    await device.Activate(CancellationToken.None);

    var deviceSettings = await _deviceCache.GetCachedDeviceSettings(config.UniqueId);
    if(deviceSettings is not null && deviceSettings.Fields.Count != 0)
    {
      try
      {
        await device.UpdateSettings(deviceSettings);
      }

      catch(Exception ex) 
      {
        _logger.LogError(ex.Message);
      }
    }

    await _deviceCache.CacheDeviceInfo(device);
    await _deviceCache.CacheDeviceSettings(device);

    return device;
  }
}
