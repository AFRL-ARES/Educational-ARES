using MK4S.Services;
using Radzen;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.PrusaPrinter
{
  public class PrusaMK4SUnitControlViewModel : UsbDeviceUnitViewModel
  {
    private readonly MK4SPrinterRpc.MK4SPrinterRpcClient _client;
    private readonly CancellationTokenSource _stateUpdateTokenSource = new();
    private Task _stateListener = Task.CompletedTask;
    private NotificationService _notificationService;

    public PrusaMK4SUnitControlViewModel(string deviceId, string deviceName, MK4SPrinterRpc.MK4SPrinterRpcClient client, NotificationService notificationService) : base(deviceId, deviceName)
    {
      _client = client;
      _notificationService = notificationService;
      StartStateUpdater();
    }

    public async Task MovePrinterHead()
    {
      if(string.IsNullOrWhiteSpace(MovementCommand))
      {
        await HandleMovementException();
        return;
      }

      var splitLocation = MovementCommand.Split(",");
      if(splitLocation.Length < 3 || splitLocation.Length > 4)
      {
        await HandleMovementException();
        return;
      }

      else if(splitLocation.Length == 3)
      {
        if(splitLocation.Any(val => val == string.Empty))
        {
          await HandleMovementException();
          return;
        }

        await _client.MoveAsync(new MoveRequest()
        {
          PrinterName = DeviceName,
          XCoordinate = splitLocation[0].Trim(),
          YCoordinate = splitLocation[1].Trim(),
          ZCoordinate = splitLocation[2].Trim(),
          DwellTime = "-1"
        });
      }

      else
      {
        await _client.MoveAsync(new MoveRequest()
        {
          PrinterName = DeviceName,
          XCoordinate = splitLocation[0].Trim(),
          YCoordinate = splitLocation[1].Trim(),
          ZCoordinate = splitLocation[2].Trim(),
          DwellTime = splitLocation[3].Trim()
        });
      }
    }

    public async Task HomePrinter()
    {
      await _client.HomePrinterAsync(new MK4SRequest() { PrinterName = DeviceName });
    }

    public async Task UpdateTemperatures()
    {
      var response = await _client.UpdateTempsAsync(new MK4SRequest() { PrinterName = DeviceName });
      BedTemperature = response.BedTemp;
      NozzleTemperature = response.NozzleTemp;
      Connected = response.IsConnected;
    }

    private void StartStateUpdater()
    {
      _stateListener = Task.Factory.StartNew(async _ =>
      {
        while(!_stateUpdateTokenSource.Token.IsCancellationRequested)
        {
          await UpdateTemperatures();
          await Task.Delay(TimeSpan.FromSeconds(2));
        }
      },
      _stateUpdateTokenSource.Token,
      TaskCreationOptions.LongRunning);
    }

    public async ValueTask DisposeAsync()
    {
      _stateUpdateTokenSource.Cancel();
      await _stateListener;
      _stateListener.Dispose();
      _stateUpdateTokenSource.Dispose();
      GC.SuppressFinalize(this);
    }

    private Task HandleMovementException()
    {
      var notif = new NotificationMessage();
      notif.Severity = NotificationSeverity.Error;
      notif.Detail = "Couldn't execute movement command, check format of coordinates. Dwell time is optional, but X,Y,Z are required.";
      notif.Summary = "Movement Request Error";
      notif.Duration = 5000.0;

      _notificationService.Notify(notif);
      return Task.CompletedTask;
    }

    [Reactive]
    public string? MovementCommand { get; set; }

    [Reactive]
    public double BedTemperature { get; set; }

    [Reactive]
    public double NozzleTemperature { get; set; }

    [Reactive]
    public bool Connected { get; set; }
  }
}
