using Ares.Datamodel;
using Ares.Datamodel.Planning;
using DynamicData;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Planning;

public class PlannerServiceCache(IDbContextFactory<CoreDatabaseContext> _dbContextFactory) : IPlannerServiceCache
{
  public async Task<AresStruct?> GetCachedPlannerSettings(string plannerId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var settings = await ctx.PlannerSettings.FirstOrDefaultAsync(settings => settings.PlannerId == plannerId);
    return settings?.Settings;
  }

  public async Task<PlannerServiceInfo?> GetCachedPlannerInfo(string plannerId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var info = await ctx.PlannerInfos.FirstOrDefaultAsync(info => info.UniqueId == plannerId);
    return info;
  }

  public async Task CachePlannerSettings(RemotePlannerService planner)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var settings = planner.Settings;
    var existingSettings = await ctx.PlannerSettings.FirstOrDefaultAsync(setting => setting.PlannerId == planner.UniqueId);

    if(existingSettings is not null)
    {
      existingSettings.Settings = settings;
      await ctx.SaveChangesAsync();
    }
    else
    {
      var newSettings = new PlannerSettings
      {
        PlannerId = planner.UniqueId,
        Settings = settings
      };
      ctx.PlannerSettings.Add(newSettings);
      await ctx.SaveChangesAsync();
    }
  }

  public async Task CachePlannerInfo(RemotePlannerService planner)
  {
    var currentInfo = await PlannerToPlannerInfo(planner);
    var ctx = _dbContextFactory.CreateDbContext();
    var cachedInfo = await ctx.PlannerInfos.FirstOrDefaultAsync(info => info.UniqueId == planner.UniqueId);

    if(cachedInfo is not null)
    {
      cachedInfo.Name = planner.Name;
      cachedInfo.Type = planner.Type;
      cachedInfo.Description = planner.Description;
      cachedInfo.Address = planner.Address.ToString();
      cachedInfo.Version = planner.Version;
      cachedInfo.Capabilities ??= new PlannerServiceCapabilities();
      var cachedCapabilities = cachedInfo.Capabilities;
      var currentCapabilities = currentInfo.Capabilities;

      var plannersAreNew = currentCapabilities.AvailablePlanners.All(p => string.IsNullOrEmpty(p.UniqueId));
      if(plannersAreNew) 
      {
        cachedCapabilities.AvailablePlanners.Clear();
        cachedCapabilities.AvailablePlanners.AddRange(currentCapabilities.AvailablePlanners);
      }

      cachedCapabilities.AcceptedTypes.Clear();
      cachedCapabilities.AcceptedTypes.AddRange(currentCapabilities.AcceptedTypes);
      cachedCapabilities.ServiceName = currentCapabilities.ServiceName;
      cachedCapabilities.SettingsSchema = currentCapabilities.SettingsSchema;
      cachedCapabilities.TimeoutSeconds = currentCapabilities.TimeoutSeconds;

      await ctx.SaveChangesAsync();
    }
    else
    {
      ctx.PlannerInfos.Add(currentInfo);
      await ctx.SaveChangesAsync();
    }
  }

  private static async Task<PlannerServiceInfo> PlannerToPlannerInfo(RemotePlannerService planner)
  {
    var capabilities = await planner.GetCapabilities();

    return new PlannerServiceInfo
    {
      Name = planner.Name,
      Type = planner.Type,
      Description = planner.Description,
      UniqueId = planner.UniqueId,
      Address = planner.Address.ToString(),
      Version = planner.Version,
      Capabilities = capabilities
    };
  }
}
