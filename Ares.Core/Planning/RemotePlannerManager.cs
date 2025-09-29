using Ares.Core.Notifications;
using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Planning;

public class RemotePlannerManager(IDbContextFactory<CoreDatabaseContext> _dbContextFactory, 
  IPlannerServiceRepo _plannerRepo, 
  INotificationHandler _notificationHandler,
  IPlannerServiceCache _plannerCache) : IRemotePlannerManager
{
  private readonly List<RemotePlannerMonitor> _plannerMonitors = [];
  private readonly string _demoPlannerUniqueId = "4b14d5e9-1c9f-4f01-8b2b-4d4d1e2e3e4e";

  public async Task CreatePlanner(string name, string url)
  {
    var config = new PlannerConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var planner = ConfigToPlanner(config);
    if(planner is null)
      return;

    _plannerRepo.AddPlanner(planner);
    var monitor = new RemotePlannerMonitor(planner, _plannerCache);
    _plannerMonitors.Add(monitor);

    var ctx = _dbContextFactory.CreateDbContext();
    ctx.Planners.Add(config);

    await ctx.SaveChangesAsync();
  }

  public Task CreateDemoPlanner(string url)
  {
    var config = new PlannerConfig { UniqueId = _demoPlannerUniqueId, Name = "Demo Remote Planner", Url = url };
    var planner = ConfigToPlanner(config);
    if(planner is null)
      return Task.CompletedTask;

    _plannerRepo.AddPlanner(planner);
    var monitor = new RemotePlannerMonitor(planner, _plannerCache);
    _plannerMonitors.Add(monitor);

    return Task.CompletedTask;
  }

  private RemotePlannerService? ConfigToPlanner(PlannerConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _ = _notificationHandler.HandleNotification(
        "Planner Load Error",
        $"Failed to load a remote planner {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Danger);
      return null;
    }

    var planner = new RemotePlannerService(config.Name, uri, config.UniqueId);

    return planner;
  }

  private async Task<RemotePlannerService?> LoadExistingPlanner(PlannerConfig config)
  {
    var planner = ConfigToPlanner(config);
    if(planner is null)
      return null;

    try
    {
      var plannerInfo = await _plannerCache.GetCachedPlannerInfo(config.UniqueId);
      if(plannerInfo is not null)
      {
        await planner.UpdateInfo(plannerInfo);
      }

      await planner.Init();

      var plannerSettings = await _plannerCache.GetCachedPlannerSettings(config.UniqueId);
      if(plannerSettings is not null)
      {
        planner.UpdateSettings(plannerSettings);
      }

      await _plannerCache.CachePlannerInfo(planner);
      await _plannerCache.CachePlannerSettings(planner);
    }

    catch(Exception e)
    {
      await planner.SetOfflinePlannerStatus(e.Message);
    }

    return planner;
  }

  public async Task LoadPlanners()
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var configs = await ctx.Planners.ToArrayAsync();
    var planners = await Task.WhenAll(configs.Select(LoadExistingPlanner));
    var nonNullPlanners = planners.OfType<RemotePlannerService>().ToArray();
    foreach(var planner in nonNullPlanners)
    {
      _plannerRepo.AddPlanner(planner);
      var monitor = new RemotePlannerMonitor(planner, _plannerCache);
      _plannerMonitors.Add(monitor);
    }
  }

  public async Task RemovePlanner(string plannerId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var planner = ctx.Planners.Where(a => a.UniqueId == plannerId).FirstOrDefault();
    if(planner is null)
    {
      return;
    }

    ctx.Remove(planner);
    await ctx.SaveChangesAsync();

    _plannerRepo.RemovePlanner(plannerId);
    var monitor = _plannerMonitors.First(m => m.PlannerId == plannerId);
    monitor.Dispose();
    _plannerMonitors.Remove(monitor);
  }

  public async Task UpdatePlanner(PlannerConfig config)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var plannerConfig = ctx.Planners.Where(a => a.UniqueId == config.UniqueId).FirstOrDefault();
    if(plannerConfig is null)
      return;
    

    plannerConfig.Name = config.Name;
    plannerConfig.Url = config.Url;
    await ctx.SaveChangesAsync();

    _plannerRepo.RemovePlanner(plannerConfig.UniqueId);
    var monitor = _plannerMonitors.First(m => m.PlannerId == plannerConfig.UniqueId);
    monitor.Dispose();
    _plannerMonitors.Remove(monitor);
    var planner = await LoadExistingPlanner(plannerConfig);
    if(planner is null)
    {
      return;
    }

    monitor = new RemotePlannerMonitor(planner, _plannerCache);
    _plannerMonitors.Add(monitor);
    _plannerRepo.AddPlanner(planner);
  }

  public Task UpdatePlannerSettings(PlannerSettings plannerSettings)
  {
    var planner = _plannerRepo.GetPlannerById(plannerSettings.PlannerId);
    if(planner is null)
      return Task.CompletedTask;
    

    planner.UpdateSettings(plannerSettings.Settings);

    if(planner is not RemotePlannerService remotePlanner)
      return Task.CompletedTask;

    return _plannerCache.CachePlannerSettings(remotePlanner);
  }
}
