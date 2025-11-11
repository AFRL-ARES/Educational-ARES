using Ares.Core.AresEnvironment;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device.USB;
using MK4S.Config;
using MK4S.Services;
using PrusaMK4S.Handlers;
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

  public async Task<MK4SRequestResponse> MovePrinter(int x, int y, int z, int dwell)
  {
    XPosition = x.ToString();
    YPosition = y.ToString(); 
    ZPosition = z.ToString();

    if(dwell == -1)
    {
      await Task.Delay(TimeSpan.FromSeconds(5));
      return new MK4SRequestResponse() { Success = true };
    }
    else
    {
      await Task.Delay(TimeSpan.FromSeconds(dwell + 5));
      return new MK4SRequestResponse() { Success = true };
    }
  }

  public void PopulateCredentials(MK4SConfig config)
  {
    Username = config.Username;
    Password = config.Password;
    //Address = new Uri(config.Address);
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

    return Task.FromResult(new PrintTempsResponse() { BedTemp = BedTemperature, NozzleTemp = NozzleTemperature, IsConnected = true });
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

  public async Task<uint> SmartCalculateNumberOfPrints(byte[] gcode)
  {
    var handler = new GCodeHandler(gcode);
    await handler.Init();
    var result = await handler.SmartDetermineNumberOfPrints();
    await handler.DisposeAsync();
    return result;
  }

  public Task SetSmartPrintMode(bool smartPrintMode)
  {
    SmartPrintMode = smartPrintMode;
    return Task.CompletedTask;
  }

  public async Task<MK4SRequestResponse> Print(byte[] gcode,
    int nozzleTemp, 
    int bedTemp, 
    double extrusionMod, 
    double speedMod, 
    int retractionLength, 
    double accelerationMod,
    double fanSpeedMod)
  {
    var response = new MK4SRequestResponse();

    var handler = new GCodeHandler(gcode);
    await handler.Init();
    LatestGCodeHandler = handler;

    if(!SmartPrintMode)
    {
      //If this is the case, the student has opted not to utilize our auto calculation and clearing the print bed must be done manually.
      var modifiedGcode = await handler.ApplyPlanningParameters(bedTemp, nozzleTemp, extrusionMod, speedMod, retractionLength, accelerationMod, fanSpeedMod, gcode);
      var request = CreatePrintRequest(modifiedGcode);
      Console.WriteLine("Successfully created a print request!");
    }

    else
    {
      //More complicated, as we need to determine the location of our print.
      var parsed = int.TryParse(AresEnvironment.GetInternalVariable(InternalVariableType.CurrentExperimentNumber), out var expNumber);

      if(!parsed)
      {
        response.ErrorString = "Printer couldn't determine iteration number for smart print, unable to complete print!";
        return response;
      }

      //Customize our G Code
      var custom_gcode = await handler.CreatePrintIteration(expNumber);
      var modifiedGcode = await handler.ApplyPlanningParameters(bedTemp, nozzleTemp, extrusionMod, speedMod, retractionLength, accelerationMod, fanSpeedMod, custom_gcode);

      File.WriteAllBytes($"Iteration_Test_{expNumber}.gcode", modifiedGcode);
      var printJobRequest = CreatePrintRequest(modifiedGcode);
      Console.WriteLine("Successfully created a print request!");
    }

    //Allow the printer some time to process our command.
    Thread.Sleep(TimeSpan.FromSeconds(5));

    response.Success = true;
    return response;
  }

  private HttpRequestMessage CreatePrintRequest(byte[] fileBytes)
  {
    var requestAddress = new Uri($"https://localhost:7800/api/v1/files/usb/aresPrint.gcode");
    var printJobRequest = new HttpRequestMessage(HttpMethod.Put, requestAddress);
    var content = new ByteArrayContent(fileBytes);
    var contentLength = fileBytes.Length;
    printJobRequest.Content = content;
    printJobRequest.Headers.Add("Accept-Language", "en");
    printJobRequest.Headers.Add("Accept", "application/json");
    printJobRequest.Headers.Add("Print-After-Upload", "true");
    printJobRequest.Headers.Add("Overwrite", "?1");
    content.Headers.ContentLength = contentLength;
    return printJobRequest;
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public async Task<MK4SRequestResponse> MoveToLastPrint(int z, int dwell, int xOffset, int yOffset)
  {
    //Attempts to move the print head over the last known print location
    //Use the last GCodeHandler to try and determine our x and y positioning

    if(LatestGCodeHandler is null)
      return new MK4SRequestResponse { Success = false, ErrorString = "Determining the last print location requires existing G-Code knowledge, which wasn't found" };

    var itemWidth = LatestGCodeHandler.ItemWidth;
    var itemHeight = LatestGCodeHandler.ItemHeight;

    //Use the GCodeHandler to gather the latest shift values. Add half the items respective width or height to place the camera around the middle of the object
    var calculated_y = LatestGCodeHandler.GetPrintBedHeight() - (Math.Abs(LatestGCodeHandler.LatestYShift) + (itemHeight / 2)) + yOffset;
    var calculated_x = LatestGCodeHandler.LatestXShift + (itemWidth / 2) + xOffset;

    return await MovePrinter((int)calculated_x, (int)calculated_y, z, dwell);
  }

  public double BedTemperature { get; set; } = 50;
  public double NozzleTemperature { get; set; } = 120;
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string XPosition { get; set; } = "0";
  public string YPosition { get; set; } = "0";
  public string ZPosition { get; set; } = "0";
  public bool SmartPrintMode { get; set; }
  public Uri? Address { get; set; }
  public IObservable<HttpResponseMessage?> StateStream { get; set; }
  public IGcodeHandler? LatestGCodeHandler { get; set; }
}
