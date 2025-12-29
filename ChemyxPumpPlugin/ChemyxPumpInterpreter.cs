using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Ares.Device;
using ChemyxPumpPlugin.Commands;

namespace ChemyxPumpPlugin;

public class ChemyxPumpInterpreter : DeviceCommandInterpreter<ChemyxPump, ChemyxPumpCommand>
{
  private const int DefaultPump = 1;
  private const int DefaultStartMode = 0;

  public ChemyxPumpInterpreter(ChemyxPump device) : base(device)
  {
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(ChemyxPumpCommand deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    var result = new CommandResult { Success = true };

    // helper to read numeric parameter by index
    double? GetNumberParam(string name, double? fallback = null)
    {
      var param = parameters.FirstOrDefault(p => p.Metadata.Name == name);
      return param != null && param.Value.HasNumberValue ? param.Value.NumberValue : fallback;
    }

    var pumpIndex = (int?)GetNumberParam(nameof(PumpIndex), DefaultPump);

    switch(deviceCommandEnum)
    {
      case ChemyxPumpCommand.StartPump:
        var mode = (int?)GetNumberParam(nameof(Mode), DefaultStartMode) ?? DefaultStartMode;
        await Device.Start(pumpIndex, mode);
        break;

      case ChemyxPumpCommand.StopPump:
        await Device.Stop(pumpIndex);
        break;

      case ChemyxPumpCommand.PausePump:
        await Device.Pause(pumpIndex);
        break;

      case ChemyxPumpCommand.PumpStatus:
        var status = Device.GetStatus(pumpIndex);
        result.Result = status.HasValue ? AresStructHelper.CreateNumberStruct("Status", (int)status.Value) : AresStructHelper.CreateNullStruct("Status");
        break;

      case ChemyxPumpCommand.SetDiameter:
        {
          var value = GetNumberParam(nameof(Value));
          if(!value.HasValue)
            return Failure("SetDiameter requires a numeric Value parameter.");
          var response = await Device.SetDiameter(value.Value, pumpIndex);
          result.Result = response.HasValue ? AresStructHelper.CreateNumberStruct("Diameter", response.Value) : AresStructHelper.CreateNullStruct("Diameter");
          break;
        }

      case ChemyxPumpCommand.DispensedVolume:
        {
          var value = Device.GetDispensedVolume(pumpIndex);
          result.Result = value.HasValue ? AresStructHelper.CreateNumberStruct("Volume", value.Value) : AresStructHelper.CreateNullStruct("Volume");
          break;
        }

      case ChemyxPumpCommand.ElapsedTime:
        {
          var value = Device.GetElapsedTime(pumpIndex);
          result.Result = value.HasValue ? AresStructHelper.CreateNumberStruct("ElapsedMinutes", value.Value.TotalMinutes) : AresStructHelper.CreateNullStruct("Minutes");
          break;
        }

      case ChemyxPumpCommand.SetVolume:
        {
          var value = GetNumberParam(nameof(Value));
          if(!value.HasValue)
            return Failure("SetVolume requires a numeric Value parameter.");
          var response = await Device.SetVolume(value.Value, pumpIndex);
          result.Result = response.HasValue ? AresStructHelper.CreateNumberStruct("Volume", response.Value) : AresStructHelper.CreateNullStruct("Volume");
          break;
        }

      case ChemyxPumpCommand.ReadLimitParameter:
        {
          var program = (int?)GetNumberParam(nameof(ProgramIndex), 0) ?? 0;
          var values = Device.ReadLimitParameter(pumpIndex);
          if(values is null)
          {
            result.Result = AresStructHelper.CreateNullStruct("Limits");
            break;
          }
          result.Result = AresStructHelper.CreateNumberStruct("MaxRate", values.MaxRate)
            .AddNumber("MinRate", values.MinRate)
            .AddNumber("MaxVolume", values.MaxVolume)
            .AddNumber("MinVolume", values.MinVolume);
          break;
        }

      case ChemyxPumpCommand.SetRate:
        {
          var value = GetNumberParam(nameof(Value));
          if(!value.HasValue)
            return Failure("SetRate requires a numeric Value parameter.");
          var response = await Device.SetRate(value.Value, pumpIndex);
          result.Result = response.HasValue ? AresStructHelper.CreateNumberStruct("Rate", response.Value) : AresStructHelper.CreateNullStruct("Rate");
          break;
        }


      case ChemyxPumpCommand.SetDelay:
        {
          var value = GetNumberParam(nameof(Value));
          if(!value.HasValue)
            return Failure("SetDelay requires a numeric Value parameter.");
          var delayReq = TimeSpan.FromSeconds(value.Value);
          var response = await Device.SetDelay(delayReq, pumpIndex);

          if(!response.HasValue)
          {
            result.Result = AresStructHelper.CreateNullStruct("Delay");
          }
          else
          {
            result.Result = AresStructHelper.CreateNumberStruct("Delay", response.Value.TotalSeconds);
          }
          break;
        }

      case ChemyxPumpCommand.SetTime:
        {
          var value = GetNumberParam(nameof(Value));
          if(!value.HasValue)
            return Failure("SetTime requires a numeric Value parameter.");

          var timeSpan = TimeSpan.FromMinutes(value.Value);
          var response = await Device.SetTime(timeSpan, pumpIndex);
          if(response.HasValue)
          {
            var responseTime = response.Value.time.TotalMinutes;
            result.Result = AresStructHelper.CreateNumberStruct("Rate", response.Value.rate)
              .AddNumber("Time", responseTime);
          }
          else
          {
            result.Result = AresStructHelper.CreateNullStruct("SetTime");
          }
          break;
        }

      case ChemyxPumpCommand.SetUnits:
        {
          var value = GetNumberParam(nameof(Value));
          if(!value.HasValue)
            return Failure("SetUnits requires a numeric Value parameter.");

          var units = (PumpUnits)(int)value.Value;
          var response = await Device.SetUnits(units, pumpIndex);
          result.Result = response.HasValue ? AresStructHelper.CreateNumberStruct("Units", response.Value) : AresStructHelper.CreateNullStruct("Units");
          break;
        }

      default:
        return Failure($"Unsupported command {deviceCommandEnum}");
    }

    return result;
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return
    [
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.StartPump),
        Description = "Start pump (mode 0 basic).",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) },
          new ParameterMetadata { Index = 1, Name = nameof(Mode), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) }
        }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.StopPump),
        Description = "Stop pump.",
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) } }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.PausePump),
        Description = "Pause pump.",
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) } }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.PumpStatus),
        Description = "Get pump status.",
        OutputMetadata = new OutputMetadata { Description = "Status code", DataSchema = AresSchemaHelper.CreateSchema("Status", AresDataType.Number), Index = 0 },
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) } }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.SetDiameter),
        Description = "Set syringe diameter (mm).",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) },
          new ParameterMetadata { Index = 1, Name = nameof(Value), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) }
        }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.DispensedVolume),
        Description = "Get dispensed volume.",
        OutputMetadata = new OutputMetadata { Description = "Volume", DataSchema = AresSchemaHelper.CreateSchema("Volume", AresDataType.Number), Index = 0 },
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) } }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.ElapsedTime),
        Description = "Get elapsed time (minutes).",
        OutputMetadata = new OutputMetadata { Description = "ElapsedMinutes", DataSchema = AresSchemaHelper.CreateSchema("Minutes", AresDataType.Number), Index = 0 },
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) } }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.SetVolume),
        Description = "Set target volume.",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) },
          new ParameterMetadata { Index = 1, Name = nameof(Value), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) }
        }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.ReadLimitParameter),
        Description = "Read limit parameters.",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) }
        },
        OutputMetadata = new OutputMetadata
        {
          Description = "Limit parameters",
          DataSchema = AresSchemaHelper.CreateSchema("MaxRate", AresDataType.Number)
            .AddEntry("MinRate", AresDataType.Number)
            .AddEntry("MaxVolume", AresDataType.Number)
            .AddEntry("MinVolume", AresDataType.Number),
          Index = 0
        }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.SetRate),
        Description = "Set rate.",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) },
          new ParameterMetadata { Index = 1, Name = nameof(Value), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) }
        }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.SetDelay),
        Description = "Set start delay (minutes).",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) },
          new ParameterMetadata { Index = 1, Name = nameof(Value), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) }
        }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.SetTime),
        Description = "Set run time (minutes).",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) },
          new ParameterMetadata { Index = 1, Name = nameof(Value), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) }
        }
      },
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = nameof(ChemyxPumpCommand.SetUnits),
        Description = "Set rate units (0=mL/min,1=mL/hr,2=uL/min,3=uL/hr).",
        ParameterMetadatas =
        {
          new ParameterMetadata { Index = 0, Name = nameof(PumpIndex), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false) },
          new ParameterMetadata { Index = 1, Name = nameof(Value), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) }
        }
      }
    ];
  }

  private CommandResult Failure(string message) => new CommandResult { Success = false, Error = message };

  // Parameter names
  private const string PumpIndex = "PumpIndex";
  private const string Mode = "Mode";
  private const string Value = "Value";
  private const string ProgramIndex = "ProgramIndex";
}
