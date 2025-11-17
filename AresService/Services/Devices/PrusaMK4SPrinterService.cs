using Ares.Core.Device;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using MK4S.Config;
using MK4S.Services;
using PrusaMK4S;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EducationalAresService.Services.Devices;

public class PrusaMK4SPrinterService : MK4SPrinterRpc.MK4SPrinterRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<MK4SConfig, IPrusaMK4S> _deviceManager;
  private readonly IDeviceConfigManager<MK4SConfig> _configManager;
  private readonly ILogger<PrusaMK4SPrinterService> _logger;

  public PrusaMK4SPrinterService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<MK4SConfig, IPrusaMK4S> deviceManager,
    IDeviceConfigManager<MK4SConfig> configManager,
    ILogger<PrusaMK4SPrinterService> logger)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
    _logger = logger;
  }

  private IPrusaMK4S? GetPrinter(string id)
  {
    var printer = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IPrusaMK4S>()
      .FirstOrDefault(p => p.UniqueId == id);

    return printer;
  }

  public override async Task<NumberOfPrintsResponse> CalculateNumberOfPrints(CalculateNumberOfPrintsRequest request, ServerCallContext context)
  {

    var response = new NumberOfPrintsResponse();
    response.NumberOfExperiments = 0;
    var gcodeBytes = request.Gcode.ToArray();
    var printer = GetPrinter(request.Id);

    if(printer is null)
      return response;

    response.NumberOfExperiments = await printer.SmartCalculateNumberOfPrints(gcodeBytes);
    _logger.LogInformation($"Smart Print Calculation Performed. Determined {response.NumberOfExperiments} prints");
    return response;
  }

  public override Task<Empty> SetSmartPrintMode(SmartPrintModeRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);

    if(request.ShouldSmartPrint)
      _logger.LogInformation("Smart Print Mode Enabled");

    else
      _logger.LogInformation("Smart Print Mode Disabled");

    if(printer is not null)
      printer.SetSmartPrintMode(request.ShouldSmartPrint);

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> StartStateUpdater(StartStateUpdaterRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);
    if(printer is not null)
      printer.StartStateUpdater(request.Interval?.ToTimeSpan() ?? TimeSpan.FromMilliseconds(250));

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> StopStateUpdater(MK4SRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);

    if(printer is not null)
      printer.StartStateUpdater();

    return Task.FromResult(new Empty());
  }

  public override async Task<PrintTempsResponse> UpdateTemps(MK4SRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);

    if(printer is not null)
      return await printer.GetAndUpdateState();

    return new PrintTempsResponse() { BedTemp = -1.0, NozzleTemp = -1.0 };
  }

  public override async Task<MK4SRequestResponse> Print(PrintRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);
    var response = new MK4SRequestResponse();
    if(printer is null)
    {
      response.Success = false;
      response.ErrorString = $"ARES could not find a printer with the ID {request.Id}!";
      _logger.LogInformation(response.ErrorString);
      return response;
    }

    await printer.Print(request.Gcode.ToArray(), -1, -1, -1, -1, -1, -1, -1);
    response.Success = true;
    response.ErrorString = null;
    _logger.LogInformation("Finished a print job successfully");
    return response;
  }

  public override async Task<MK4SRequestResponse> HomePrinter(MK4SRequest request, ServerCallContext context)
  {
    _logger.LogInformation("Received a printer home request.");
    var printer = GetPrinter(request.Id);
    if(printer is not null)
      return await printer.HomePrinter();

    return new MK4SRequestResponse() { ErrorString = $"ARES could not find a printer named {request.Id}!" };
  }

  public override async Task<MK4SRequestResponse> MoveToLastPrint(MoveToLastPrintRequest request, ServerCallContext context)
  {
    _logger.LogInformation("Received a request to move to last print.");

    var printer = GetPrinter(request.Id);

    _logger.LogInformation($"Determined a dwell time of {request.DwellTime}");

    if(printer is not null)
      return await printer.MoveToLastPrint(request.ZCoordinate, request.DwellTime, 0, 0);

    var errorStr = $"ARES could not find a printer with the ID {request.Id}";
    _logger.LogInformation(errorStr);
    return new MK4SRequestResponse() { ErrorString = errorStr };
  }

  public override async Task<MK4SRequestResponse> Move(MoveRequest request, ServerCallContext context)
  {
    _logger.LogInformation("Received a request to move printer head.");
    var printer = GetPrinter(request.Id);

    if(printer is not null)
      return await printer.MovePrinter(request.XCoordinate, request.YCoordinate, request.ZCoordinate, request.DwellTime);

    var error = $"ARES could not find a printer with the ID {request.Id}";
    _logger.LogInformation(error);
    return new MK4SRequestResponse() { ErrorString = error };
  }

  public override async Task<Empty> AddMK4SPrinter(MK4SConfig request, ServerCallContext context)
  {
    var printer = await _deviceManager.Create(request);
    await _configManager.Add(printer.UniqueId, printer.Name, request);
    if(printer is not null)
      printer.PopulateCredentials(request);

    return new Empty();
  }

  public override async Task<Empty> RemoveMK4SPrinter(MK4SRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.Id);
    await _configManager.Remove(request.Id);
    return new Empty();
  }

  public override Task<GetAllMK4SPrintersResponse> GetAllMK4SPrinters(Empty request, ServerCallContext context)
  {
    var printerDescriptions = _deviceCommandInterpreterRepo
      .Select(deviceInterpreter => deviceInterpreter.Device)
      .OfType<IPrusaMK4S>()
      .Select(printer => new PrinterDescription { Id = printer.UniqueId, Name = printer.Name });

    var response = new GetAllMK4SPrintersResponse();
    response.Printers.AddRange(printerDescriptions);
    return Task.FromResult(response);
  }

  public override Task<Empty> UpdateMK4SPrinter(UpdatePrinterRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);

    if(printer is not null)
      printer.PopulateCredentials(request.NewConfig);

    _deviceManager.Update(request.Id, request.NewConfig);
    _configManager.Update(request.Id, request.NewConfig);
    return Task.FromResult(new Empty());
  }
}
