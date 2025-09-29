using Ares.Datamodel.Device;
using Ares.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.State.Logging;

public class StateLoggerManager(IDeviceStateLoggerRepository _stateLoggerRepository, IEnumerable<IDeviceStateLoggerFactory> _factories, ILogger<StateLoggerManager> _logger, IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
{
  private bool _overrideActive;
  private readonly SemaphoreSlim _overrideLock = new(1, 1);

  public async Task SetupLogger(IAresDevice device)
  {
    // Stop and remove any existing logger for the device
    if(_stateLoggerRepository.TryGetValue(device.UniqueId, out var existingLogger))
    {
      await existingLogger.Stop();
      _stateLoggerRepository.Remove(device.UniqueId);
    }

    var factory = _factories.FirstOrDefault(f => f.CanHandle(device));
    if(factory is null)
    {
      _logger.LogError("No suitable logger factory found for device type {DeviceType}", device.GetType().Name);
      return;
    }

    var logger = factory.Create(device);

    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == device.UniqueId);

    await logger.Start(existingSettings);
    _stateLoggerRepository.Add(device.UniqueId, logger);
  }

  public async Task RemoveLogger(string deviceId)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      await logger.Stop();
      _stateLoggerRepository.Remove(deviceId);
    }

    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
    if(existingSettings is not null)
    {
      ctx.DeviceLoggingSettings.Remove(existingSettings);
    }

    _logger.LogInformation("Removed logger for device id {DeviceId}", deviceId);
  }

  public async Task UpdateLogger(string deviceId, DeviceLoggingSettings settings)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      await _overrideLock.WaitAsync();

      await UpdateDatabase(deviceId, settings);

      try
      {
        if(!_overrideActive)
        {
          await logger.UpdateSettings(settings);
          _logger.LogInformation("Updated logger for device id {DeviceId}", deviceId);
        }
      }
      catch(Exception e)
      {
        _logger.LogError("Error updating device state logger for device {DeviceId}: {Exception}", deviceId, e);
      }
      finally
      {
        _overrideLock.Release();
      }
    }
    else
    {
      throw new KeyNotFoundException($"No logger found for device {deviceId}");
    }
  }

  private async Task UpdateDatabase(string deviceId, DeviceLoggingSettings settings)
  {
    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
    if(existingSettings is not null)
    {
      existingSettings.IntervalMs = settings.IntervalMs;
      existingSettings.LoggingType = settings.LoggingType;
      existingSettings.Deltas.Clear();
      existingSettings.Deltas.Add(settings.Deltas);
    }
    else
    {
      ctx.DeviceLoggingSettings.Add(settings);
    }

    await ctx.SaveChangesAsync();
  }

  public DeviceLoggingSettings GetCurrentLoggerSettings(string deviceId)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      return logger.Settings;
    }

    throw new KeyNotFoundException($"No logger found for device {deviceId}");
  }

  public async Task<DeviceLoggingSettings?> GetDatabaseLoggerSettings(string deviceId)
  {
    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
    return existingSettings;
  }

  public async Task DisableOverrideAsync()
  {
    await _overrideLock.WaitAsync();

    if(!_overrideActive)
    {
      _overrideLock.Release();
      return;
    }

    try
    {
      var loggerRestoreTasks = _stateLoggerRepository.Select(async logger =>
      {
        var settings = await GetDatabaseLoggerSettings(logger.Key);
        await logger.Value.UpdateSettings(settings);
      });

      await Task.WhenAll(loggerRestoreTasks);
      _logger.LogInformation("Disabled state logging override");
    }
    catch(Exception e)
    {
      _logger.LogError("Error disabling state logging override: {Exception}", e);
    }
    finally
    {
      _overrideLock.Release();
    }
  }

  public async Task EnableOverrideAsync(DeviceLoggingSettings settings)
  {
    await _overrideLock.WaitAsync();

    _overrideActive = true;

    try
    {
      var loggerOverrideTasks = _stateLoggerRepository
        .Select(async logger => await logger.Value.UpdateSettings(settings));

      await Task.WhenAll(loggerOverrideTasks);
      _logger.LogInformation("Enabled state logging override");
    }
    catch(Exception e)
    {
      _logger.LogError("Error enabling state logging override: {Exception}", e);
    }
    finally
    {
      _overrideLock.Release();
    }
  }
}
