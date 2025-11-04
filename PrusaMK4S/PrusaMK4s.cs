using Ares.Core.AresEnvironment;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device.USB;
using MK4S.Config;
using MK4S.Services;
using PrusaMK4S.Commands.Responses;
using PrusaMK4S.Handlers;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;

namespace PrusaMK4S;

public class PrusaMK4s : AresUSBDevice, IPrusaMK4S
{
  private HttpClient? _httpClient;
  private readonly ISubject<HttpResponseMessage?> _stateSubject = new BehaviorSubject<HttpResponseMessage?>(default);
  private CancellationTokenSource _internalStateUpdaterTokenSource = new();
  private Task? _stateUpdater;

  private string _stateRequestAddress = string.Empty;

  public PrusaMK4s(string name) : base(name)
  {
    StateStream = _stateSubject.AsObservable();
  }

  public void PopulateCredentials(MK4SConfig config)
  {
    Username = config.Username;
    Password = config.Password;

    if(_httpClient is not null)
      _httpClient.Dispose();

    var credentialsCache = new NetworkCredential(Username, Password, "DOM");
    var authHandler = new HttpClientHandler()
    {
      PreAuthenticate = true,
      Credentials = credentialsCache,
      AllowAutoRedirect = true,
      UseDefaultCredentials = false
    };

    Address = new Uri(config.Address);

    _httpClient = new HttpClient(authHandler, true);
    _httpClient.BaseAddress = Address;
    _httpClient.Timeout = TimeSpan.FromSeconds(15);
    _stateRequestAddress = $"{Address}api/printer";
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    //TODO: Add checks for proper credential info, get printer information.
    await StartStateUpdater();
    Status = new DeviceOperationalStatus();
    Status.OperationalState = OperationalState.Active;
    Status.Message = "Activated MK4S Printer";

    return true;
  }

  public async Task<uint> SmartCalculateNumberOfPrints(byte[] gcode)
  {
    var handler = new GCodeHandler(gcode);
    await handler.Init();
    var result = await handler.SmartDetermineNumberOfPrints();
    await handler.DisposeAsync();
    return result;
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public async Task<MK4SRequestResponse> Print(byte[] gcode, int nozzleTemp, int bedTemp, double extrusionMod,
    double speedMod, int retractionLength, double accelerationMod)
  {
    var response = new MK4SRequestResponse();

    if(IsBusy)
      //If busy, wait the last set delay time plus a few seconds to try and print
      await Task.Delay(TimeSpan.FromSeconds(PrintDelay + 5));


    try
    {
      if(_httpClient is null || Address is null)
      {
        response.ErrorString = "Printer HTTP client was null, cannot send commands!";
        return response;
      }

      var handler = new GCodeHandler(gcode);
      await handler.Init();
      LatestGCodeHandler = handler;
      HttpResponseMessage? httpResponse;

      if(!SmartPrint)
      {
        //If this is the case, the student has opted not to utilize our auto calculation and clearing the print bed must be done manually.
        var modifiedGcode = await handler.ApplyPlanningParameters(bedTemp, nozzleTemp, extrusionMod, speedMod, retractionLength, accelerationMod, gcode);
        var request = CreatePrintRequest(modifiedGcode);
        httpResponse = await _httpClient.SendAsync(request);
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
        var modifiedGcode = await handler.ApplyPlanningParameters(bedTemp, nozzleTemp, extrusionMod, speedMod, retractionLength, accelerationMod, custom_gcode);

        await File.WriteAllBytesAsync($"Iteration_Test_{expNumber}.gcode", modifiedGcode);
        var printJobRequest = CreatePrintRequest(modifiedGcode);
        httpResponse = await _httpClient.SendAsync(printJobRequest);
      }

      if(!httpResponse.IsSuccessStatusCode)
      {
        response.ErrorString = httpResponse.ReasonPhrase;
        return response;
      }

      //Allow the printer some time to process our command.
      Thread.Sleep(TimeSpan.FromSeconds(5));

      while(IsPrinting || IsBusy)
        Thread.Sleep(5000);

      response.Success = true;
      return response;
    }

    catch(Exception ex)
    {
      response.Success = false;
      response.ErrorString = ex.Message;
      return response;
    }
  }

  public async Task<MK4SRequestResponse> MoveToLastPrint(string z, string dwell, int xOffset, int yOffset)
  {
    try
    {
      //Attempts to move the print head over the last known print location
      //Use the last GCodeHandler to try and determine our x and y positioning
      if(LatestGCodeHandler is null)
        return new MK4SRequestResponse { Success = false, ErrorString = "Determining the last print location requires existing G-Code knowledge, which wasn't found" };

      var itemWidth = LatestGCodeHandler.ItemWidth;
      var itemHeight = LatestGCodeHandler.ItemHeight;

      //Use the GCodeHandler to gather the latest shift values. Add half the items respective width or height to place the camera around the middle of the object
      var calculated_y = LatestGCodeHandler.PrintBedHeight - (Math.Abs(LatestGCodeHandler.LatestYShift) + (itemHeight / 2)) + yOffset;
      var calculated_x = LatestGCodeHandler.LatestXShift + (itemWidth / 2) + xOffset;

      return await MovePrinter(calculated_x.ToString(), calculated_y.ToString(), z, dwell);
    }

    catch(Exception ex)
    {
      var response = new MK4SRequestResponse
      {
        Success = false,
        ErrorString = ex.Message
      };

      return response;
    }
  }

  public async Task<MK4SRequestResponse> MovePrinter(string x, string y, string z, string dwell)
  {
    if(string.IsNullOrEmpty(z))
      z = "110";

    var response = new MK4SRequestResponse();
    try
    {
      if(_httpClient is null)
      {
        response.ErrorString = "Printer HTTP client was null, cannot send commands!";
        return response;
      }

      string movementCommand;
      var parsed = int.TryParse(dwell, out var dwellInt);

      if(!parsed)
        PrintDelay = 10;

      else
        PrintDelay = dwellInt;

      if(dwellInt > 0)
        movementCommand = $"G90 \n G1 X{x} Y{y} Z{z} F9000 \n G4 S{PrintDelay}";
      else
        movementCommand = $"G90 \n G1 X{x} Y{y} Z{z} F9000";

      var movementRequest = CreateMovementRequest(Encoding.UTF8.GetBytes(movementCommand));
      var result = await _httpClient.SendAsync(movementRequest);

      if(result.IsSuccessStatusCode)
      {
        response.Success = true;
        return response;
      }

      else
      {
        response.ErrorString = result.ReasonPhrase;
        return response;
      }
    }

    catch(Exception ex)
    {
      response.Success = false;
      response.ErrorString = ex.Message;
      return response;
    }
  }

  public async Task<MK4SRequestResponse> HomePrinter()
  {
    var response = new MK4SRequestResponse();

    try
    {
      if(_httpClient is null)
      {
        response.ErrorString = "Printer HTTP client was null, cannot send commands!";
        return response;
      }

      var request = CreateHomeRequest(Encoding.UTF8.GetBytes("G28"));
      var result = await _httpClient.SendAsync(request);

      while(IsPrinting || IsBusy)
        continue;

      if(result.IsSuccessStatusCode)
      {
        response.Success = true;
        return response;
      }

      else
      {
        response.ErrorString = result.ReasonPhrase;
        return response;
      }
    }

    catch(Exception ex)
    {
      response.Success = false;
      response.ErrorString = ex.Message;
      return response;
    }
  }

  private HttpRequestMessage CreateHomeRequest(byte[] commandBytes)
  {
    var requestAddress = new Uri($"{Address}api/v1/files/usb/home.gcode");
    var printJobRequest = new HttpRequestMessage(HttpMethod.Put, requestAddress);
    var content = new ByteArrayContent(commandBytes);
    var contentLength = commandBytes.Length;
    printJobRequest.Content = content;
    printJobRequest.Headers.Add("Accept-Language", "en");
    printJobRequest.Headers.Add("Accept", "application/json");
    printJobRequest.Headers.Add("Print-After-Upload", "true");
    printJobRequest.Headers.Add("Overwrite", "?1");
    content.Headers.ContentLength = contentLength;
    return printJobRequest;
  }

  private HttpRequestMessage CreateMovementRequest(byte[] commandBytes)
  {
    var requestAddress = new Uri($"{Address}api/v1/files/usb/moveCommand.gcode");
    var printJobRequest = new HttpRequestMessage(HttpMethod.Put, requestAddress);
    var content = new ByteArrayContent(commandBytes);
    var contentLength = commandBytes.Length;
    printJobRequest.Content = content;
    printJobRequest.Headers.Add("Accept-Language", "en");
    printJobRequest.Headers.Add("Accept", "application/json");
    printJobRequest.Headers.Add("Print-After-Upload", "true");
    printJobRequest.Headers.Add("Overwrite", "?1");
    content.Headers.ContentLength = contentLength;
    return printJobRequest;
  }

  private HttpRequestMessage CreatePrintRequest(byte[] fileBytes)
  {
    var requestAddress = new Uri($"{Address}api/v1/files/usb/aresPrint.gcode");
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

  public ValueTask DisposeAsync()
  {
    _httpClient?.Dispose();
    return ValueTask.CompletedTask;
  }

  public async Task<PrintTempsResponse> GetAndUpdateState()
  {
    var response = new PrintTempsResponse();
    if(_httpClient is null)
      return response;

    var requestAddress = $"{Address}api/printer";
    var jsonResponse = await _httpClient.GetAsync(requestAddress);
    _stateSubject.OnNext(jsonResponse);
    var parsedResponse = JsonSerializer.Deserialize<StatusResponse>(await jsonResponse.Content.ReadAsStreamAsync());

    if(parsedResponse is null)
      return response;

    IsPrinting = parsedResponse.State.Flags.Printing;
    IsBusy = parsedResponse.State.Flags.Busy;
    response.BedTemp = parsedResponse.Temperature.Bed.Actual;
    response.NozzleTemp = parsedResponse.Temperature.Tool.Actual;
    response.IsConnected = true;
    return response;
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
    await StartStateUpdater(TimeSpan.FromSeconds(3));
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
      try
      {
        while(!token.IsCancellationRequested && _httpClient is not null)
        {
          try
          {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, _stateRequestAddress));
            _stateSubject.OnNext(response);
          }

          catch(TimeoutException)
          {
          }

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

  public Task SetSmartPrintMode(bool smartPrintMode)
  {
    SmartPrint = smartPrintMode;
    return Task.CompletedTask;
  }

  public Uri? Address { get; set; }
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public double BedTemperature { get; set; }
  public double NozzleTemperature { get; set; }
  public bool IsPrinting { get; set; }
  public bool IsBusy { get; set; }
  public bool SmartPrint { get; set; }
  public IObservable<HttpResponseMessage?> StateStream { get; }
  public IGcodeHandler? LatestGCodeHandler { get; set; }
  public int PrintDelay { get; set; } = 10; //Print Delay in Seconds
}
