using Ares.Device.Serial;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tc0304.DataModel;
using TC0304.Commands;

namespace TC0304;

public class DataloggerThermometer : SerialDevice<IDataloggerThermometerConnection>, IDataloggerThermometer
{
  private readonly ISubject<DataResponse?> _stateSubject = new BehaviorSubject<DataResponse?>(default);
  private CancellationTokenSource _internalStateUpdateTokenSource = new();
  private Task? _stateUpdater;

  public DataloggerThermometer(string name, IDataloggerThermometerConnection connection) : base(name, connection)
  {
    StateStream = _stateSubject.AsObservable();
  }

  public ProbeNames ProbeNames { get; } = new()
  {
    T1Name = "Probe 1",
    T2Name = "Probe 2",
    T3Name = "Probe 3",
    T4Name = "Probe 4"
  };

  public IObservable<DataResponse?> StateStream { get; }

  public async Task<DataResponse> GetAndUpdateState()
  {
    var response = await Connection.Send(new DataRequest());
    _stateSubject.OnNext(response);
    return response;
  }

  public async Task<double?[]> GetTemperatures()
  {
    var response = await GetAndUpdateState();
    var temperatures = new double?[]
    {
      response.T1Probe?.DegreesCelsius,
      response.T2Probe?.DegreesCelsius,
      response.T3Probe?.DegreesCelsius,
      response.T4Probe?.DegreesCelsius
    };

    return temperatures;
  }

  public DataResponse? GetState()
    => StateStream.Take(1).Wait();

  public async Task StartStateUpdater(TimeSpan interval)
  {
    await StopStateUpdater();
    _internalStateUpdateTokenSource = new CancellationTokenSource();
    await StartStateUpdater(interval, _internalStateUpdateTokenSource.Token);
  }

  public async Task StartStateUpdater()
  {
    await StopStateUpdater();
    await StartStateUpdater(TimeSpan.FromMilliseconds(250));
  }

  public async Task StopStateUpdater()
  {
    _internalStateUpdateTokenSource.Cancel();
    if(_stateUpdater is not null)
      await _stateUpdater;
  }

  public void Hold()
  {
    Connection.Send(new HoldCommand());
  }

  public async ValueTask DisposeAsync()
  {
    _internalStateUpdateTokenSource.Cancel();
    if(_stateUpdater is not null)
      await _stateUpdater;
    _internalStateUpdateTokenSource.Dispose();
    _stateSubject.OnCompleted();
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    var activated = await base.Activate(ct);
    if(!activated)
      return false;

    await StartStateUpdater();
    return true;
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    //No real safety concerns here
    return Task.CompletedTask;
  }

  public void ToggleTemperatureUnit()
  {
    Connection.Send(new ToggleTemperatureUnitCommand());
  }

  private async Task StartStateUpdater(TimeSpan interval, CancellationToken token)
  {
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = "Datalogger State Updater Thread";
      try
      {
        while(!token.IsCancellationRequested)
        {
          try
          {
            var response = await Connection.Send(new DataRequest(), TimeSpan.FromSeconds(5));
            _stateSubject.OnNext(response);
          }
          catch(TimeoutException)
          { }
          await Task.Delay(interval, token);
        }
      }
      catch(ObjectDisposedException)
      {
      }
    },
      token,
      TaskCreationOptions.LongRunning);
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      await Connection.Send(new DataRequest(), TimeSpan.FromSeconds(5));
      return new SerialDeviceValidationResult(true);
    }
    catch(Exception e)
    {
      return new SerialDeviceValidationResult(false, e.Message);
    }
  }
}
