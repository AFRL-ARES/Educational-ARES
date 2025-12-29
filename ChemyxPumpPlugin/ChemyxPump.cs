using Ares.Device.Serial;
using ChemyxPumpPlugin.Commands;
using ChemyxPumpPlugin.Commands.Requests;
using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin;

public class ChemyxPump : SerialDevice<IChemyxPumpConnection>, IChemyxPump
{
  private const int DefaultPumpIndex = 1;

  private CancellationTokenSource _internalPollToken = new();

  private Task _internalPollers = Task.CompletedTask;

  public ChemyxPump(string name, bool dualPump, IChemyxPumpConnection connection) : base(name, connection)
  {
    DualPump = dualPump;
  }
  public async Task Start(int? pump = null, int mode = 0)
    => await Connection.Send(new StartCommand(pump, mode));

  public async Task Stop(int? pump = null)
    => await Connection.Send(new StopCommand(pump));

  public async Task Pause(int? pump = null)
    => await Connection.Send(new PauseCommand(pump));

  private async Task<PumpStatus?> PollStatus(int? pump = null)
  {
    try
    {
      var response = await Connection.Send(new PumpStatusCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(5));
      if(response is null)
        return null;

      return response.Status;
    }
    catch(Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }

    return null;

  }

  private async Task<double?> PollDispensedVolume(int? pump = null)
  {
    var response = await Connection.Send(new DispensedVolumeCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(5));
    return response.Value;
  }

  private async Task<TimeSpan?> PollElapsedTime(int? pump = null)
  {
    var response = await Connection.Send(new ElapsedTimeCommand(pump ?? DefaultPumpIndex), TimeSpan.FromSeconds(5));
    if(response?.Value is not double minutes)
    {
      return null;
    }
    var timeSpan = TimeSpan.FromMinutes(minutes);
    return timeSpan;
  }

  private async Task<LimitParameterResponse?> PollLimitParameter(int? pump = null, int program = 0)
  {
    var response = await Connection.Send(new ReadLimitParameterCommand(pump ?? DefaultPumpIndex, program), TimeSpan.FromSeconds(5));
    if(response is null)
      return null;

    return response;
  }

  public async Task<double?> SetDiameter(double diameter, int? pump = null)
  {
    var response = await Connection.Send(new SetDiameterCommand(pump ?? DefaultPumpIndex, diameter), TimeSpan.FromSeconds(5));
    ViewParameters = await PollViewParameters();
    return response.Value;
  }

  public async Task<double?> SetRate(double rate, int? pump = null)
  {
    var response = await Connection.Send(new SetRateCommand(pump ?? DefaultPumpIndex, rate), TimeSpan.FromSeconds(5));
    ViewParameters = await PollViewParameters();
    return response.Value;
  }

  public async Task<double?> SetVolume(double volume, int? pump = null)
  {
    var response = await Connection.Send(new SetVolumeCommand(pump ?? DefaultPumpIndex, volume), TimeSpan.FromSeconds(5));
    ViewParameters = await PollViewParameters();
    return response.Value;
  }

  public async Task<double?> SetUnits(PumpUnits units, int? pump = null)
  {
    var response = await Connection.Send(new SetUnitsCommand(pump ?? DefaultPumpIndex, units), TimeSpan.FromSeconds(5));
    ViewParameters = await PollViewParameters();
    return response.Value;
  }

  public async Task<TimeSpan?> SetDelay(TimeSpan delay, int? pump = null)
  {
    var response = await Connection.Send(new SetDelayCommand(pump ?? DefaultPumpIndex, delay.TotalSeconds), TimeSpan.FromSeconds(5));
    if(response?.Value is not double responseSeconds)
    {
      return null;
    }
    ViewParameters = await PollViewParameters();
    var responseTime = TimeSpan.FromSeconds(responseSeconds);
    return responseTime;
  }

  public async Task<(double rate, TimeSpan time)?> SetTime(TimeSpan time, int? pump = null)
  {
    var totalMinutes = time.TotalMinutes;
    var response = await Connection.Send(new SetTimeCommand(pump ?? DefaultPumpIndex, totalMinutes), TimeSpan.FromSeconds(5));
    if(response is null || !response.Rate.HasValue || !response.Time.HasValue)
      return null;

    ViewParameters = await PollViewParameters();
    var responseTimespan = TimeSpan.FromMinutes(response.Time.Value);
    return (response.Rate.Value, responseTimespan);
  }

  private async Task<ViewParametersResponse?> PollViewParameters()
    => await Connection.Send(new ViewParameterCommand(), TimeSpan.FromSeconds(5));

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await Stop(null);
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      var status = await PollStatus(DefaultPumpIndex);
      var valid = status.HasValue;
      return new SerialDeviceValidationResult(valid, valid ? string.Empty : "Unable to query pump status.");
    }
    catch(Exception ex)
    {
      return new SerialDeviceValidationResult(false, ex.Message);
    }
  }

  public ValueTask DisposeAsync()
  {
    throw new NotImplementedException();
  }

  public void StartPolling()
  {
    _internalPollToken = new CancellationTokenSource();
    _internalPollers = Task.WhenAll(PollParams(_internalPollToken.Token), PollState(_internalPollToken.Token));
  }

  public async Task StopPolling()
  {
    await _internalPollToken.CancelAsync();
    try
    {
      await _internalPollers;
    }
    catch(OperationCanceledException)
    { }
  }

  private Task PollParams(CancellationToken token)
  {
    return Task.Run(async () =>
    {
      while(!token.IsCancellationRequested)
      {
        try
        {
        ViewParameters = await PollViewParameters();
        await Task.Delay(TimeSpan.FromSeconds(30), token);
      }
        catch(Exception ex)
        {
          Console.WriteLine(ex);
        }
      }
    }, token);
  }

  private Task PollState(CancellationToken token)
  {
    return Task.Run(async () =>
    {
      while(!token.IsCancellationRequested)
      {
        try
        {
          var pumpStatuses = new List<PumpStatus>();
          var status = await PollStatus(1);
          if(status.HasValue)
          {
            pumpStatuses.Add(status.Value);
          }

          var dispensedVolumes = new List<double>();
          var dispensed = await PollDispensedVolume(1);
          if(dispensed.HasValue)
          {
            dispensedVolumes.Add(dispensed.Value);
          }

          var elapsedTimes = new List<TimeSpan>();
          var elapsed = await PollElapsedTime(1);
          if(elapsed.HasValue)
          {
            elapsedTimes.Add(elapsed.Value);
          }

          var limitParameters = new List<LimitParameterResponse>();
          var limit = await PollLimitParameter(1);
          if(limit is not null)
          {
            limitParameters.Add(limit);
          }

          status = await PollStatus(2);
          if(status.HasValue)
            pumpStatuses.Add(status.Value);

          dispensed = await PollDispensedVolume(2);
          if(dispensed.HasValue)
            dispensedVolumes.Add(dispensed.Value);

          elapsed = await PollElapsedTime(2);
          if(elapsed.HasValue)
            elapsedTimes.Add(elapsed.Value);

          limit = await PollLimitParameter(2);
          if(limit is not null)
            limitParameters.Add(limit);

          PumpStatuses = pumpStatuses.Any() ? pumpStatuses.ToArray() : null;
          DispensedVolumes = dispensedVolumes.Any() ? dispensedVolumes.ToArray() : null;
          ElapsedTimes = elapsedTimes.Any() ? elapsedTimes.ToArray() : null;
          LimitParameters = limitParameters.Any() ? limitParameters.ToArray() : null;
          await Task.Delay(TimeSpan.FromMilliseconds(750), token);
        }
        catch(Exception ex)
        {
          Console.WriteLine(ex);
        }
      }
    }, token);
  }

  public PumpStatus[]? PumpStatuses { get; private set; }

  public double[]? DispensedVolumes { get; private set; }

  public TimeSpan[]? ElapsedTimes { get; private set; }

  public LimitParameterResponse[]? LimitParameters { get; private set; }

  public ViewParametersResponse? ViewParameters { get; private set; }

  public PumpStatus? GetStatus(int? pump = null)
  {
    if(PumpStatuses is null || PumpStatuses.Length == 0 || pump > PumpStatuses.Count())
      return PumpStatus.Stopped;

    pump -= 1;
    return PumpStatuses?[pump ?? 0];
  }

  public double? GetDispensedVolume(int? pump = null)
  {
    if(DispensedVolumes is null || DispensedVolumes.Length == 0 || pump > DispensedVolumes.Count())
      return 0;

    pump -= 1;
    return DispensedVolumes?[pump ?? 0];
  }

  public TimeSpan? GetElapsedTime(int? pump = null)
  {
    if(ElapsedTimes is null || ElapsedTimes.Length == 0 || pump > ElapsedTimes.Count())
      return TimeSpan.FromSeconds(0);

    pump -= 1;
    return ElapsedTimes?[pump ?? 0];
  }

  public LimitParameterResponse? ReadLimitParameter(int? pump = null)
  {
    pump -= 1;
    return LimitParameters?[pump ?? 0];
  }

  public bool DualPump { get; set; } = false;
}
