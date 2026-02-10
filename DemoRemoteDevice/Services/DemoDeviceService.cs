using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Device.Remote;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemoteDevice.Services;
public class DemoDeviceService : AresRemoteDeviceService.AresRemoteDeviceServiceBase
{
  private readonly ILogger<DemoDeviceService> _logger;
  private readonly DemoDevice _device;

  public DemoDeviceService(ILogger<DemoDeviceService> logger, DemoDevice device)
  {
    _logger = logger;
    _device = device;
  }

  public override Task<Empty> EnterSafeMode(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("Safe mode activated.");
    return Task.FromResult(new Empty());
  }

  public override Task<DeviceOperationalStatus> GetOperationalStatus(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("Operational status requested, returning {}", OperationalState.Active);
    return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Active });
  }

  public override Task<DeviceInfoResponse> GetInfo(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("Info Requested");
    var response = new DeviceInfoResponse()
    {
      Name = "Demo Device",
      Version = "1.0.1",
      Description = "Me name demo device. I demo (:"
    };

    return Task.FromResult(response);
  }

  public override Task<CommandsResponse> GetCommands(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("Commands requested");
    var response = new CommandsResponse();
    var echoCommand = new DeviceCommandDescriptor
    {
      Name = Commands.ECHO_NUMBER.ToString(),
      Description = "Gives back the input number as the output",
      InputSchema = AresSchemaBuilder.Create(DemoDataTypes.InputNumber.Key, DemoDataTypes.InputNumber.Value.Type).Build(),
      OutputSchema = AresSchemaBuilder.Create(DemoDataTypes.OutputNumber.Key, DemoDataTypes.OutputNumber.Value.Type).Build()
    };
    response.Commands.Add(echoCommand);

    return Task.FromResult(response);
  }

  public override Task<DeviceExecutionResult> ExecuteCommand(ExecuteCommandRequest request, ServerCallContext context)
  {
    _logger.LogInformation("Execute command requested: {command}", request.CommandName);

    if(request.CommandName == Commands.ECHO_NUMBER.ToString())
    {
      var arg = request.Arguments.Fields.GetValueOrDefault(DemoDataTypes.InputNumber.Key);
      if(arg == default)
      {
        return Task.FromResult(new DeviceExecutionResult { Success = false, Error = $"Arg {DemoDataTypes.InputNumber.Key} not provided." });
      }
      if(!arg.HasNumberValue)
      {
        return Task.FromResult(new DeviceExecutionResult { Success = false, Error = $"Arg {DemoDataTypes.InputNumber.Key} was not a number." });
      }

      return Task.FromResult(new DeviceExecutionResult { Success = true, Result = AresStructHelper.CreateNumberStruct(DemoDataTypes.OutputNumber.Key, arg.NumberValue) });
    }

    return Task.FromResult(new DeviceExecutionResult
    {
      Success = false,
      Error = $"Unsupported command: {request.CommandName}"
    });
  }

  public override Task<SettingsSchemaResponse> GetSettingsSchema(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("Settings schema requested");

    var response = new SettingsSchemaResponse();
    var schema = new AresDataSchema();
    schema.Fields.Add(DemoDataTypes.RandomTags.Key, DemoDataTypes.RandomTags.Value);
    schema.Fields.Add(DemoDataTypes.PreselectedTags.Key, DemoDataTypes.PreselectedTags.Value);

    response.Schema = schema;
    return Task.FromResult(response);
  }

  public override Task<CurrentSettingsResponse> GetCurrentSettings(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("Current settings requested");

    var response = new CurrentSettingsResponse
    {
      Settings = _device.Settings
    };
    return Task.FromResult(response);
  }

  public override Task<Empty> SetSettings(SetSettingsRequest request, ServerCallContext context)
  {
    _logger.LogInformation("Set settings requested");

    foreach(var setting in request.Settings.Fields)
    {
      _device.Settings.Fields[setting.Key] = setting.Value;
    }

    return Task.FromResult(new Empty());
  }

  public override Task<StateSchemaResponse> GetStateSchema(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("State schema requested");
    var schema = AresSchemaBuilder.Create("Temperature", AresDataType.Number).Build();
    var response = new StateSchemaResponse
    {
      Schema = schema
    };

    return Task.FromResult(response);
  }

  public override Task<DeviceStateResponse> GetState(Empty request, ServerCallContext context)
  {
    _logger.LogInformation("State requested");
    var state = AresStructHelper.CreateNumberStruct("Temperature", _device.Temperature);
    var response = new DeviceStateResponse
    {
      State = state
    };
    return Task.FromResult(response);
  }

  public override async Task GetStateStream(DeviceStateStreamRequest request, IServerStreamWriter<DeviceStateResponse> responseStream, ServerCallContext context)
  {
    _logger.LogInformation("State stream requested with interval {interval}ms and type of {pollingtype}", request.PollingSettings.IntervalMs, request.PollingSettings.PollingType);
    while(!context.CancellationToken.IsCancellationRequested)
    {
      var state = AresStructHelper.CreateNumberStruct("Temperature", _device.Temperature);
      _logger.LogInformation("Sending back a state from the state stream {}", state);
      var response = new DeviceStateResponse
      {
        State = state
      };
      await responseStream.WriteAsync(response);
      try
      {
        await Task.Delay(TimeSpan.FromMilliseconds(request.PollingSettings.IntervalMs > 0 ? request.PollingSettings.IntervalMs : 1000), context.CancellationToken);
      }
      catch (TaskCanceledException)
      {}
    }
  }
}