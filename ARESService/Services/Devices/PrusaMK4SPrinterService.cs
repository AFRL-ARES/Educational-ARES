using Ares.Core.Device;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
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

  public PrusaMK4SPrinterService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<MK4SConfig, IPrusaMK4S> deviceManager,
    IDeviceConfigManager<MK4SConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
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
    return response;
  }

  public override Task<Empty> SetSmartPrintMode(SmartPrintModeRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);
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
      return response;
    }

    await printer.Print(request.Gcode.ToArray(), -1, -1, -1, -1, -1, -1);
    response.Success = true;
    response.ErrorString = null;
    return response;
  }

  public override async Task<MK4SRequestResponse> HomePrinter(MK4SRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);
    if(printer is not null)
      return await printer.HomePrinter();

    return new MK4SRequestResponse() { ErrorString = $"ARES could not find a printer named {request.Id}!" };

  }

  public override async Task<MK4SRequestResponse> MoveToLastPrint(MoveToLastPrintRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);

    var dwell = "-1";

    if(!string.IsNullOrEmpty(request.DwellTime))
      dwell = request.DwellTime;

    if(printer is not null)
      return await printer.MoveToLastPrint(request.ZCoordinate, dwell);

    return new MK4SRequestResponse() { ErrorString = $"ARES could not find a printer with the ID {request.Id}" };
  }

  public override async Task<MK4SRequestResponse> Move(MoveRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.Id);
    var dwell = "-1";

    if(!string.IsNullOrEmpty(request.DwellTime))
      dwell = request.DwellTime;

    if(printer is not null)
      return await printer.MovePrinter(request.XCoordinate, request.YCoordinate, request.ZCoordinate, dwell);

    return new MK4SRequestResponse() { ErrorString = $"ARES could not find a printer with the ID {request.Id}" };
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
