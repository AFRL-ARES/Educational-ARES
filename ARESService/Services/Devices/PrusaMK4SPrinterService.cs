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

  private IPrusaMK4S? GetPrinter(string name)
  {
    var printer = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IPrusaMK4S>()
      .FirstOrDefault();

    return printer;
  }

  public override async Task<NumberOfPrintsResponse> CalculateNumberOfPrints(CalculateNumberOfPrintsRequest request, ServerCallContext context)
  {
    var response = new NumberOfPrintsResponse();
    response.NumberOfExperiments = 0;
    var unpacked = request.DeviceCommand.TryUnpack<BytesValue>(out var bytes);
    var printer = GetPrinter(request.DeviceName);

    if(!unpacked || printer is null)
      return response;

    response.NumberOfExperiments = await printer.SmartCalculateNumberOfPrints(bytes.Value.ToByteArray());
    return response;
  }

  public override Task<Empty> SetSmartPrintMode(SmartPrintModeRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.DeviceName);
    if(printer is not null)
      printer.SetSmartPrintMode(request.ShouldSmartPrint);

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> StartStateUpdater(StartStateUpdaterRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.DeviceName);
    if(printer is not null)
      printer.StartStateUpdater(request.Interval?.ToTimeSpan() ?? TimeSpan.FromMilliseconds(250));

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> StopStateUpdater(MK4SRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.PrinterName);

    if(printer is not null)
      printer.StartStateUpdater();

    return Task.FromResult(new Empty());
  }

  public override async Task<PrintTempsResponse> UpdateTemps(MK4SRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.PrinterName);

    if(printer is not null)
      return await printer.GetAndUpdateState();

    return new PrintTempsResponse() { BedTemp = -1.0, NozzleTemp = -1.0 };
  }

  public override async Task<MK4SRequestResponse> Print(PrintRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.PrinterName);
    var response = new MK4SRequestResponse();
    if(printer is null)
    {
      response.Success = false;
      response.ErrorString = $"ARES could not find a printer named {request.PrinterName}!";
      return response;
    }

    await printer.Print(request.Gcode.ToArray(), -1, -1, -1, -1, -1, -1);
    response.Success = true;
    response.ErrorString = null;
    return response;
  }

  public override async Task<MK4SRequestResponse> HomePrinter(MK4SRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.PrinterName);
    if(printer is not null)
      return await printer.HomePrinter();

    return new MK4SRequestResponse() { ErrorString = $"ARES could not find a printer named {request.PrinterName}!" };

  }

  public override async Task<MK4SRequestResponse> Move(MoveRequest request, ServerCallContext context)
  {
    var printer = GetPrinter(request.PrinterName);
    var dwell = "-1";

    if(!string.IsNullOrEmpty(request.DwellTime))
      dwell = request.DwellTime;

    if(printer is not null)
      return await printer.MovePrinter(request.XCoordinate, request.YCoordinate, request.ZCoordinate, dwell);

    return new MK4SRequestResponse() { ErrorString = $"ARES could not find a printer named {request.PrinterName}" };
  }

  public override async Task<Empty> AddMK4SPrinter(MK4SConfig request, ServerCallContext context)
  {
    await _deviceManager.Load(request.DeviceId, request);
    var printer = GetPrinter(request.DeviceName);

    if(printer is not null)
      printer.PopulateCredentials(request);
    await _configManager.Add(request.DeviceId, request.DeviceName, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveMK4SPrinter(MK4SRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.PrinterName);
    await _configManager.Remove(request.PrinterName);
    return new Empty();
  }

  public override Task<GetAllMK4SPrintersResponse> GetAllMK4SPrinters(Empty request, ServerCallContext context)
  {
    var printers = _deviceCommandInterpreterRepo
      .Select(deviceInterpreter => deviceInterpreter.Device)
      .OfType<IPrusaMK4S>();

    var response = new GetAllMK4SPrintersResponse();
    response.Printers.AddRange(printers.Select(p => new PrinterDescription { Id = p.UniqueId, Name = p.Name }));
    return Task.FromResult(response);
  }

  public override Task<Empty> UpdateMK4SPrinter(MK4SConfig request, ServerCallContext context)
  {
    var printer = GetPrinter(request.DeviceName);
    if(printer is not null)
      printer.PopulateCredentials(request);

    _deviceManager.Update(request.DeviceId, request);
    _configManager.Update(request.DeviceName, request);
    return Task.FromResult(new Empty());
  }
}
