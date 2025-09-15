using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Core.Device;
using Ares.Core.Device.Remote;
using Ares.Core.Grpc;
using Ares.Core.Planning;
using Ares.Services;
using AresService.DeviceDbLoaders;
using DemoDevice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AresService;

public class AresStarter
{
  private readonly IRemoteAnalyzerManager _analyzerManager;
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IEnumerable<IDeviceDbLoader> _deviceLoaders;
  private readonly IRemotePlannerManager _plannerManager;
  private readonly IConfiguration _configuration;
  private readonly IRemoteDeviceManager _remoteDeviceManager;
  private readonly string _dataPath;
  private readonly string _resultsPath;
  private readonly string _templatesPath;
  private readonly string _devicesPath;

  public AresStarter(
    IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDbContextFactory<AresDbContext> dbContextFactory,
    IRemotePlannerManager plannerManager,
    IRemoteAnalyzerManager analyzerManager,
    IEnumerable<IDeviceDbLoader> deviceLoaders,
    IConfiguration configuration,
    IRemoteDeviceManager remoteDeviceManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _dbContextFactory = dbContextFactory;
    _plannerManager = plannerManager;
    _analyzerManager = analyzerManager;
    _deviceLoaders = deviceLoaders;
    _configuration = configuration;
    _remoteDeviceManager = remoteDeviceManager;
    _dataPath = _configuration.Get<AppSettings>().AresDataPath;
    _resultsPath = Path.Combine(_dataPath, AppSettings.ResultsFolder);
    _templatesPath = Path.Combine(_dataPath, AppSettings.TemplatesFolder);
    _devicesPath = Path.Combine(_dataPath, AppSettings.DevicesFolder);
  }

  public async Task Start()
  {
    await EnsureDataPathsExist();

#if DEBUG
    //await AddDemoDevice(new Uri("https://localhost:7038"));
    //await _plannerManager.CreateDemoPlanner("http://localhost:5036");
    //await _analyzerManager.CreateDemoAnalyzer("http://localhost:5026");
#endif

    foreach(var deviceLoader in _deviceLoaders)
      await deviceLoader.Load();

    await _plannerManager.LoadPlanners();
    await _analyzerManager.LoadAnalyzers();
    await _remoteDeviceManager.LoadDevices();

    Observable.Interval(TimeSpan.FromSeconds(20))
      .Take(1)
      .Subscribe(_ => ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Error, StatusMessage = "This is a test error from server." }));
  }

  public Task EnsureDataPathsExist()
  {
    Directory.CreateDirectory(_devicesPath);
    Directory.CreateDirectory(_resultsPath);
    Directory.CreateDirectory(_templatesPath);

    return Task.CompletedTask;
  }

  public Task AddDemoDevice(Uri address)
  {
    var testDevice = new AresDemoDevice(address);
    testDevice.Activate(CancellationToken.None);
    var testDeviceInterpreter = new DemoDeviceInterpreter(testDevice);
    _deviceCommandInterpreterRepo.Add(testDeviceInterpreter);
    return Task.CompletedTask;
  }
}
