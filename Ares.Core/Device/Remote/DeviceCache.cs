using Ares.Datamodel;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device.Remote;
internal class DeviceCache(IDbContextFactory<CoreDatabaseContext> _dbContextFactory) : IDeviceCache
{
  public async Task CacheDeviceInfo(RemoteDevice device)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var currentInfo = DeviceToDeviceInfo(device);
    var existingInfoInDb = await ctx.DeviceInfos.FirstOrDefaultAsync(info => info.UniqueId == device.UniqueId);

    if(existingInfoInDb is not null)
    {
      existingInfoInDb.Name = device.Name;
      existingInfoInDb.Type = device.Type;
      existingInfoInDb.Description = device.Description;
      existingInfoInDb.Url = device.Address.ToString();
      existingInfoInDb.Version = device.Version;
      existingInfoInDb.SettingsSchema = device.SettingSchema;
      existingInfoInDb.Commands.Clear();
      existingInfoInDb.Commands.AddRange(device.CommandDescriptors);
    }
    else
    {
      ctx.DeviceInfos.Add(currentInfo);
    }

    await ctx.SaveChangesAsync();
  }

  public async Task CacheDeviceSettings(RemoteDevice device)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var settings = new AresStruct();
    settings.Fields.Add(device.Settings);

    var existingSettings = await ctx.DeviceSettings.FirstOrDefaultAsync(s => s.DeviceId == device.UniqueId);

    if(existingSettings is not null)
    {
      existingSettings.Settings = settings;
    }
    else
    {
      var newSettings = new DeviceSettings { DeviceId = device.UniqueId, Settings = settings };
      ctx.DeviceSettings.Add(newSettings);
    }

    await ctx.SaveChangesAsync();
  }

  public async Task<DeviceInfo?> GetCachedDeviceInfo(string deviceId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var info = await ctx.DeviceInfos.FirstOrDefaultAsync(info => info.UniqueId == deviceId);
    return info;
  }

  public async Task<AresStruct?> GetCachedDeviceSettings(string deviceId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var settings = await ctx.DeviceSettings.FirstOrDefaultAsync(settings => settings.DeviceId == deviceId);
    return settings?.Settings;
  }

  private static DeviceInfo DeviceToDeviceInfo(RemoteDevice device)
  {
    var info = new DeviceInfo
    {
      UniqueId = device.UniqueId,
      Name = device.Name,
      Type = device.Type,
      Description = device.Description,
      Version = device.Version,
      Url = device.Address.ToString(),
      SettingsSchema = device.SettingSchema,
    };
    info.Commands.AddRange(device.CommandDescriptors);

    return info;
  }
}
