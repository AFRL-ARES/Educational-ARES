using Ares.Datamodel.Device;
using Ares.Device.USB;
using MK4S.Config;
using MK4S.Services;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PrusaMK4S.Simulation;

public class SimPrusaMK4S : AresUSBDevice, IPrusaMK4S
{
  private readonly Random _random = new Random();
  private readonly ISubject<HttpResponseMessage?> _stateSubject = new BehaviorSubject<HttpResponseMessage?>(default);
  private CancellationTokenSource _internalStateUpdaterTokenSource = new();
  private Task? _stateUpdater;

  public SimPrusaMK4S(string deviceName) : base(deviceName)
  {
    Status = new DeviceOperationalStatus();
    StateStream = _stateSubject.AsObservable();
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    await StartStateUpdater();
    Status.OperationalState = OperationalState.Active;
    Status.Message = "Successfully activated simulated printer!";

    return true;
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  public async Task<MK4SRequestResponse> HomePrinter()
  {
    await Task.Delay(TimeSpan.FromSeconds(5));
    return new MK4SRequestResponse() { Success = true };
  }

  public async Task<MK4SRequestResponse> MovePrinter(string x, string y, string z, string dwell)
  {
    var parsed = int.TryParse(dwell, out var dwellTime);

    if(parsed)
    {
      await Task.Delay(TimeSpan.FromSeconds(dwellTime));
      return new MK4SRequestResponse() { Success = true };
    }
    else
    {
      await Task.Delay(TimeSpan.FromSeconds(5));
      return new MK4SRequestResponse() { Success = true };
    }
  }

  public void PopulateCredentials(MK4SConfig config)
  {
    Username = config.Username;
    Password = config.Password;
    Address = new Uri(config.Address);
  }

  public async Task<MK4SRequestResponse> Print(byte[] gcode, int nozzleTemp, int bedTemp)
  {
    await Task.Delay(TimeSpan.FromSeconds(10));
    return new MK4SRequestResponse() { Success = true };
  }

  public Task<PrintTempsResponse> GetAndUpdateState()
  {
    var randomChange = _random.NextDouble();
    var coin = _random.Next(2) == 0;

    if(coin)
    {
      BedTemperature += randomChange;
      NozzleTemperature -= randomChange;
    }

    else
    {
      BedTemperature -= randomChange;
      NozzleTemperature += randomChange;
    }

    BedTemperature = Math.Round(BedTemperature, 1);
    NozzleTemperature = Math.Round(NozzleTemperature, 1);

    return Task.FromResult(new PrintTempsResponse() { BedTemp = BedTemperature, NozzleTemp = NozzleTemperature });
  }

  public HttpResponseMessage? GetState()
    => StateStream.Take(1).Wait();

  public async Task StartStateUpdater(TimeSpan interval)
  {
    await StopStateUpdater();
    _internalStateUpdaterTokenSource = new CancellationTokenSource();
    await StartStateUpdater(interval, _internalStateUpdaterTokenSource.Token);
  }

  public async Task StartStateUpdater()
  {
    await StopStateUpdater();
    await StartStateUpdater(TimeSpan.FromMilliseconds(3000));
  }

  public async Task StopStateUpdater()
  {
    _internalStateUpdaterTokenSource?.Cancel();
    if(_stateUpdater is not null)
      await _stateUpdater;
  }

  public async Task StartStateUpdater(TimeSpan interval, CancellationToken token)
  {
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {
      while(!token.IsCancellationRequested)
      {
        await GetAndUpdateState();
        await Task.Delay(interval, token);
      }
    },
    token,
    TaskCreationOptions.LongRunning);
  }

  public Task<uint> SmartCalculateNumberOfPrints(byte[] gcode)
  {
    throw new NotImplementedException();
  }

  public Task SetSmartPrintMode(bool smartPrintMode)
  {
    throw new NotImplementedException();
  }

  public Task<MK4SRequestResponse> Print(byte[] gcode, int nozzleTemp, int bedTemp, double extrusionMod, double speedMod, int retractionLength, double accelerationMod)
  {
    throw new NotImplementedException();
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public double BedTemperature { get; set; } = 50;
  public double NozzleTemperature { get; set; } = 120;
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public Uri? Address { get; set; }
  public IObservable<HttpResponseMessage?> StateStream { get; set; }
}
