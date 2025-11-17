using System.Linq;
using System.Threading.Tasks;
using Ares.Device;
using AresService.Data;
using AresService.DeviceManagers;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceDbLoaders;

public abstract class DeviceDbLoaderBase<TDevice, TConfig> : IDeviceDbLoader where TDevice : IAresDevice where TConfig : IMessage, new()
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IDeviceManager<TConfig, TDevice> _deviceManager;

  public DeviceDbLoaderBase(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<TConfig, TDevice> deviceManager)
  {
    _dbContextFactory = dbContextFactory;
    _deviceManager = deviceManager;
  }

  public virtual async Task Load()
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var deviceConfigs = await context.DeviceConfigs
      .Where(config => config.DeviceType == typeof(TDevice).FullName)
      .Select(config => new LoadableConfig<TConfig>(config.UniqueId, config.ConfigData.Unpack<TConfig>())).ToArrayAsync();
    await _deviceManager.Load(deviceConfigs);
  }
}
