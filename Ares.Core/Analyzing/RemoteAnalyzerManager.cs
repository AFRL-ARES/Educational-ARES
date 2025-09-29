using Ares.Core.Notifications;
using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Analyzing;
public class RemoteAnalyzerManager(IDbContextFactory<CoreDatabaseContext> _dbContextFactory, IAnalyzerRepo _analyzerRepo, INotificationHandler _notificationHandler, IAnalyzerCache _analyzerCache) : IRemoteAnalyzerManager
{
  private readonly List<RemoteAnalyzerMonitor> _analyzerMonitors = [];
  private static readonly string _demoAnalyzerUniqueId = "9e5a8f3b-5c7d-4a1b-9f0a-1a2b3c4d5e6f";

  public async Task CreateAnalyzer(string name, string url)
  {
    var config = new AnalyzerConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var analyzer = ConfigToAnalyzer(config);
    if(analyzer is null)
      return;

    _analyzerRepo.AddAnalyzer(analyzer);
    var monitor = new RemoteAnalyzerMonitor(analyzer, _analyzerCache);
    _analyzerMonitors.Add(monitor);

    var ctx = _dbContextFactory.CreateDbContext();
    ctx.Analyzers.Add(config);

    await ctx.SaveChangesAsync();
  }

  public Task CreateDemoAnalyzer(string url)
  {
    var config = new AnalyzerConfig { UniqueId = _demoAnalyzerUniqueId, Name = "Demo Remote Analyzer", Url = url };
    var analyzer = ConfigToAnalyzer(config);
    if(analyzer is null)
      return Task.CompletedTask;

    _analyzerRepo.AddAnalyzer(analyzer);
    var monitor = new RemoteAnalyzerMonitor(analyzer, _analyzerCache);
    _analyzerMonitors.Add(monitor);

    return Task.CompletedTask;
  }

  private RemoteAnalyzer? ConfigToAnalyzer(AnalyzerConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _ = _notificationHandler.HandleNotification(
        "Analyzer Load Error",
        $"Failed to load a remote analyzer {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Danger);
      return null;
    }

    var analyzer = new RemoteAnalyzer(config.Name, uri, config.UniqueId);

    return analyzer;
  }

  private async Task<RemoteAnalyzer?> LoadExistingAnalyzer(AnalyzerConfig config)
  {
    var analyzer = ConfigToAnalyzer(config);
    if(analyzer is null)
      return null;

    var analyzerInfo = await _analyzerCache.GetCachedAnalyzerInfo(config.UniqueId);
    if(analyzerInfo is not null)
    {
      await analyzer.UpdateInfo(analyzerInfo);
    }

    await analyzer.Init();

    var analyzerSettings = await _analyzerCache.GetCachedAnalyzerSettings(config.UniqueId);
    if(analyzerSettings is not null)
    {
      analyzer.UpdateSettings(analyzerSettings);
    }

    await _analyzerCache.CacheAnalyzerInfo(analyzer);
    await _analyzerCache.CacheAnalyzerSettings(analyzer);

    return analyzer;
  }

  public async Task LoadAnalyzers()
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var configs = await ctx.Analyzers.ToArrayAsync();
    var analyzers = await Task.WhenAll(configs.Select(LoadExistingAnalyzer));
    var nonNullAnalyzers = analyzers.OfType<RemoteAnalyzer>().ToArray();
    foreach(var analyzer in nonNullAnalyzers)
    {
      _analyzerRepo.AddAnalyzer(analyzer);
      var monitor = new RemoteAnalyzerMonitor(analyzer, _analyzerCache);
      _analyzerMonitors.Add(monitor);
    }
  }

  public async Task RemoveAnalyzer(string analyzerId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var analyzer = ctx.Analyzers.Where(a => a.UniqueId == analyzerId).FirstOrDefault();
    if(analyzer is null)
    {
      return;
    }

    ctx.Remove(analyzer);
    await ctx.SaveChangesAsync();

    _analyzerRepo.RemoveAnalyzer(analyzerId);
    var monitor = _analyzerMonitors.First(m => m.AnalyzerId == analyzerId);
    monitor.Dispose();
    _analyzerMonitors.Remove(monitor);
  }

  public async Task UpdateAnalyzer(AnalyzerConfig config)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var analyzerCfg = ctx.Analyzers.Where(a => a.UniqueId == config.UniqueId).FirstOrDefault();
    if(analyzerCfg is null)
    {
      return;
    }

    analyzerCfg.Name = config.Name;
    analyzerCfg.Url = config.Url;
    await ctx.SaveChangesAsync();

    _analyzerRepo.RemoveAnalyzer(analyzerCfg.UniqueId);
    var monitor = _analyzerMonitors.First(m => m.AnalyzerId == analyzerCfg.UniqueId);
    monitor.Dispose();
    _analyzerMonitors.Remove(monitor);
    var analyzer = await LoadExistingAnalyzer(analyzerCfg);
    if(analyzer is null)
    {
      return;
    }

    monitor = new RemoteAnalyzerMonitor(analyzer, _analyzerCache);
    _analyzerMonitors.Add(monitor);
    _analyzerRepo.AddAnalyzer(analyzer);
  }

  public Task UpdateAnalyzerSettings(AnalyzerSettings analyzerSettings)
  {
    var analyzer = _analyzerRepo.GetAnalyzerById(analyzerSettings.AnalyzerId);
    if (analyzer is null)
    {
      return Task.CompletedTask;
    }

    analyzer.UpdateSettings(analyzerSettings.Settings);

    if(analyzer is not RemoteAnalyzer remoteAnalyzer)
      return Task.CompletedTask;

    return _analyzerCache.CacheAnalyzerSettings(remoteAnalyzer);
  }
}
