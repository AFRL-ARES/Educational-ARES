using System.Text;
using System.Threading;

namespace ChemyxPumpPlugin.Simulation;

public class SimChemyxPump
{
  private sealed class PumpState
  {
    public double Units { get; set; }
    public double Diameter { get; set; }
    public double Rate { get; set; }
    public double Volume { get; set; }
    public double Delay { get; set; }
    public double SetTime { get; set; }
    public double DispensedVolume { get; set; }
    public double ElapsedMinutes { get; set; }
    public PumpStatus Status { get; set; } = PumpStatus.Stopped;
  }

  private readonly Action<byte[]> _byteSender;
  private readonly object _lock = new();
  private readonly PumpState[] _pumps;
  private readonly Random _random = new();
  private readonly object _randomLock = new();
  private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
  private DateTime _lastUpdate = DateTime.UtcNow;

  public SimChemyxPump(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
    _pumps = new[]
    {
      new PumpState { Units = 0, Diameter = 30.000, Rate = 16.934912, Volume = 14.254977, Delay = 0, SetTime = 0 },
      new PumpState { Units = 0, Diameter = 13.000, Rate = 23.652897, Volume = 19.909843, Delay = 0, SetTime = 0 }
    };
    StartInternalStateUpdater();
  }

  private void StartInternalStateUpdater()
  {
    Task.Factory.StartNew(() =>
    {
      while(true)
      {
        Task.Delay(200).Wait();
        UpdatePumps();
      }
    }, TaskCreationOptions.LongRunning);
  }

  private void UpdatePumps()
  {
    lock(_lock)
    {
      var now = DateTime.UtcNow;
      var delta = (now - _lastUpdate).TotalMinutes;
      _lastUpdate = now;

      if(delta <= 0)
        return;

      foreach(var pump in _pumps)
      {
        if(pump.Status != PumpStatus.Running)
          continue;

        pump.ElapsedMinutes += delta;
        pump.DispensedVolume += pump.Rate * delta;

        if(pump.Volume > 0 && pump.DispensedVolume >= pump.Volume)
        {
          pump.DispensedVolume = pump.Volume;
          pump.Status = PumpStatus.Stopped;
        }

        if(pump.SetTime > 0 && pump.ElapsedMinutes >= pump.SetTime)
        {
          pump.ElapsedMinutes = pump.SetTime;
          pump.Status = PumpStatus.Stopped;
        }
      }
    }
  }

  public void SendCommand(byte[] command)
  {
    int delay;
    lock(_randomLock)
    {
      delay = _random.Next(50, 100);
    }

    Task.Delay(delay).ContinueWith(_ =>
    {
      var cmd = Encoding.UTF8.GetString(command);
      ProcessCommand(cmd);
    });
  }

  private void ProcessCommand(string command)
  {
    if(string.IsNullOrWhiteSpace(command))
      return;

    var trimmedCommand = command.TrimEnd('\0', '\r', '\n');
    if(string.IsNullOrWhiteSpace(trimmedCommand))
      return;

    var originalCommand = trimmedCommand;
    var tokens = trimmedCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if(tokens.Length == 0)
    {
      SendInvalid(originalCommand);
      return;
    }

    var tokenOffset = 0;
    var pumpIndex = 1;
    if(int.TryParse(tokens[0], out var parsedPump) && parsedPump is >= 1 and <= 2)
    {
      pumpIndex = parsedPump;
      tokenOffset = 1;
    }

    if(tokenOffset >= tokens.Length)
    {
      SendInvalid(originalCommand);
      return;
    }

    var cmd = tokens[tokenOffset].ToLowerInvariant();
    var args = tokens.Skip(tokenOffset + 1).ToArray();

    switch(cmd)
    {
      case "start":
        HandleStart(originalCommand, pumpIndex, tokenOffset == 0);
        break;
      case "stop":
        HandleStop(originalCommand, pumpIndex, tokenOffset == 0);
        break;
      case "pause":
        HandlePause(originalCommand, pumpIndex, tokenOffset == 0);
        break;
      case "restart":
        HandleRestart(originalCommand, pumpIndex, tokenOffset == 0);
        break;
      case "pump" when tokenOffset > 0 && args.FirstOrDefault()?.Equals("status", StringComparison.InvariantCultureIgnoreCase) == true:
        HandleStatus(originalCommand, pumpIndex);
        break;
      case "set":
        HandleSet(originalCommand, pumpIndex, args);
        break;
      case "change":
        HandleChange(originalCommand, pumpIndex, args);
        break;
      case "dispensed" when args.FirstOrDefault()?.Equals("volume", StringComparison.InvariantCultureIgnoreCase) == true:
        HandleDispensedVolume(originalCommand, pumpIndex);
        break;
      case "elapsed" when args.FirstOrDefault()?.Equals("time", StringComparison.InvariantCultureIgnoreCase) == true:
        HandleElapsedTime(originalCommand, pumpIndex);
        break;
      case "read" when args.Length >= 3 && args[0].Equals("limit", StringComparison.InvariantCultureIgnoreCase) && args[1].Equals("parameter", StringComparison.InvariantCultureIgnoreCase):
        HandleLimitParameter(originalCommand);
        break;
      case "view" when args.FirstOrDefault()?.Equals("parameter", StringComparison.InvariantCultureIgnoreCase) == true:
        HandleViewParameters(originalCommand);
        break;
      default:
        SendInvalid(originalCommand);
        break;
    }
  }

  private void HandleStart(string originalCommand, int pumpIndex, bool applyAll)
  {
    lock(_lock)
    {
      if(applyAll)
      {
        foreach(var pump in _pumps)
        {
          if(pump.Status == PumpStatus.Stopped)
          {
            pump.ElapsedMinutes = 0;
            pump.DispensedVolume = 0;
          }
          pump.Status = PumpStatus.Running;
        }
      }
      else
      {
        var pump = _pumps[pumpIndex - 1];
        if(pump.Status == PumpStatus.Stopped)
        {
          pump.ElapsedMinutes = 0;
          pump.DispensedVolume = 0;
        }
        pump.Status = PumpStatus.Running;
      }
    }

    SendResponse(originalCommand, "Pump start running...");
  }

  private void HandleStop(string originalCommand, int pumpIndex, bool applyAll)
  {
    lock(_lock)
    {
      if(applyAll)
      {
        foreach(var pump in _pumps)
          pump.Status = PumpStatus.Stopped;
      }
      else
      {
        _pumps[pumpIndex - 1].Status = PumpStatus.Stopped;
      }
    }

    SendResponse(originalCommand, "Pump stop!");
  }

  private void HandlePause(string originalCommand, int pumpIndex, bool applyAll)
  {
    lock(_lock)
    {
      if(applyAll)
      {
        foreach(var pump in _pumps)
          pump.Status = PumpStatus.Paused;
      }
      else
      {
        _pumps[pumpIndex - 1].Status = PumpStatus.Paused;
      }
    }

    SendResponse(originalCommand, "Pump pause!");
  }

  private void HandleRestart(string originalCommand, int pumpIndex, bool applyAll)
  {
    lock(_lock)
    {
      if(applyAll)
      {
        foreach(var pump in _pumps)
          pump.Status = PumpStatus.Running;
      }
      else
      {
        _pumps[pumpIndex - 1].Status = PumpStatus.Running;
      }
    }

    SendResponse(originalCommand, "Pump restart!");
  }

  private void HandleStatus(string originalCommand, int pumpIndex)
  {
    var status = 0;
    lock(_lock)
    {
      status = (int)_pumps[pumpIndex - 1].Status;
    }

    SendResponse(originalCommand, status.ToString());
  }

  private void HandleSet(string originalCommand, int pumpIndex, string[] args)
  {
    if(args.Length < 2)
    {
      SendInvalid(originalCommand);
      return;
    }

    var pump = _pumps[pumpIndex - 1];
    var setting = args[0].ToLowerInvariant();
    var valueString = string.Join(' ', args.Skip(1));

    if(!double.TryParse(valueString, out var value))
    {
      SendInvalid(originalCommand);
      return;
    }

    switch(setting)
    {
      case "units":
        lock(_lock)
        {
          pump.Units = value;
        }
        SendResponse(originalCommand, $"units = {value:0}");
        break;
      case "diameter":
        lock(_lock)
        {
          pump.Diameter = value;
        }
        SendResponse(originalCommand, $"diameter = {value:F3}");
        break;
      case "rate":
        lock(_lock)
        {
          pump.Rate = value;
        }
        SendResponse(originalCommand, $"rate = {value:F6}");
        break;
      case "volume":
        lock(_lock)
        {
          pump.Volume = value;
        }
        SendResponse(originalCommand, $"volume = {value:F6}");
        break;
      case "delay":
        lock(_lock)
        {
          pump.Delay = value;
        }
        SendResponse(originalCommand, $"delay = {value:F6}");
        break;
      case "time":
        double computedRate;
        lock(_lock)
        {
          pump.SetTime = value;
          computedRate = pump.Volume > 0 && value > 0 ? pump.Volume / value : pump.Rate;
          pump.Rate = computedRate;
        }

        SendResponse(originalCommand,
          string.Empty,
          $"rate = {computedRate:F6}",
          string.Empty,
          $"time = {value:F6}");
        break;
      default:
        SendInvalid(originalCommand);
        break;
    }
  }

  private void HandleChange(string originalCommand, int pumpIndex, string[] args)
  {
    if(args.Length < 2 || !args[0].Equals("rate", StringComparison.InvariantCultureIgnoreCase) || !double.TryParse(string.Join(' ', args.Skip(1)), out var value))
    {
      SendInvalid(originalCommand);
      return;
    }

    lock(_lock)
    {
      _pumps[pumpIndex - 1].Rate = value;
    }

    SendResponse(originalCommand, $"pump {pumpIndex} change rate = {value:F6}");
  }

  private void HandleDispensedVolume(string originalCommand, int pumpIndex)
  {
    double dispensed;
    lock(_lock)
    {
      dispensed = _pumps[pumpIndex - 1].DispensedVolume;
    }

    SendResponse(originalCommand, $"dispensed volume = {dispensed:F4}");
  }

  private void HandleElapsedTime(string originalCommand, int pumpIndex)
  {
    double elapsed;
    lock(_lock)
    {
      elapsed = _pumps[pumpIndex - 1].ElapsedMinutes;
    }

    SendResponse(originalCommand, $"elapsed time = {elapsed:0.00000}");
  }

  private void HandleLimitParameter(string originalCommand)
  {
    SendResponse(originalCommand, "125.9622 0.0002 106.0288 0.0141");
  }

  private async void HandleViewParameters(string originalCommand)
  {
    var lines = new List<string> { " " };

    lock(_lock)
    {
      for(var i = 0; i < _pumps.Length; i++)
      {
        var pump = _pumps[i];
        lines.Add($"Pump {i + 1}: ");
        lines.Add($"unit = {pump.Units:0} ");
        lines.Add($"dia = {pump.Diameter:F3} ");
        lines.Add($"rate = {pump.Rate:F6} ");
        lines.Add($"time = {pump.SetTime:0.######} ");
        lines.Add($"volume = {pump.Volume:F6} ");
        lines.Add($"delay = {pump.Delay:0.######}");
      }
    }

    await SendResponse(originalCommand, lines.ToArray());
  }

  private void SendInvalid(string originalCommand)
  {
    SendResponse(originalCommand, "Invalid Command!");
  }

  private Task SendResponse(string commandEcho, params string[] responseLines)
  {
    var sb = new StringBuilder();
    sb.Append(commandEcho);
    sb.Append('\r');

    if(responseLines is { Length: > 0 })
    {
      sb.Append(string.Join("\r\n", responseLines));
      sb.Append("\r\n");
    }

    sb.Append('>');
    var sbString = sb.ToString();
    //Console.WriteLine($"I have processed the command {commandEcho} and will be sending back a response");
    return Task.Run(async () =>
    {
      foreach(var responseChar in sbString)
      {
        _byteSender(Encoding.UTF8.GetBytes([responseChar]));
        //await Task.Delay(1);
      }
    });
  }
}
