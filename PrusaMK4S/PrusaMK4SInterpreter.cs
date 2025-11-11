
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Ares.Device;
using PrusaMK4S.Enums;

namespace PrusaMK4S;

public class PrusaMK4SInterpreter : DeviceCommandInterpreter<IPrusaMK4S, PrusaMK4SCommandType>
{
  public PrusaMK4SInterpreter(IPrusaMK4S device) : base(device)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceId = Device.UniqueId,
        Name = PrusaMK4SCommandType.Print.ToString(),
        Description = "A command that tells the printer to print with the provided G-Code source as a base file. Includes the ability to modify several parameters of " +
        "that G-Code such as nozzle temperature, extrusion rate and acceleration. For retraction length, a value of -1 will result in no changes being made in your G-Code. " +
        "For all other parameters, a value of 0 does the same. The only required parameter for this command is the G-Code itself. In the scenario you are using border objects " +
        "for machine vision purposes, you will need to provide the name of your primary object in your print file. This ensures ARES only alters G-Code associated with the " +
        "desired test object and not your border objects.",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 0,
            Name = PrusaMK4SCommandParameter.GCode.ToString(),
            NotPlannable = true,
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.ByteArray, false)
          },
          new ParameterMetadata
          {
            Index = 1,
            Name = PrusaMK4SCommandParameter.NozzleTemperature.ToString(),
            Unit = "Degree's Celsius",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          },
          new ParameterMetadata
          {
            Index = 2,
            Name = PrusaMK4SCommandParameter.BedTemperature.ToString(),
            Unit = "Degree's Celsius",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          },
          new ParameterMetadata
          {
            Index = 3,
            Name = PrusaMK4SCommandParameter.ExtrusionRateMod.ToString(),
            Unit = "Modifier",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          },
          new ParameterMetadata
          {
            Index = 4,
            Name = PrusaMK4SCommandParameter.SpeedMod.ToString(),
            Unit = "Modifier",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          },
          new ParameterMetadata
          {
            Index = 5,
            Name = PrusaMK4SCommandParameter.RetractionLength.ToString(),
            Unit = "Millimeters",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          },
          new ParameterMetadata
          {
            Index = 6,
            Name = PrusaMK4SCommandParameter.AccelerationMod.ToString(),
            Unit = "Modifier",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          },
          new ParameterMetadata
          {
            Index = 7,
            Name = PrusaMK4SCommandParameter.FanSpeedMod.ToString(),
            Unit = "Modifier",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          }
        },
        OutputMetadata = new OutputMetadata()
        {
          Description = "Returns whether or not the print was successfully executed.",
          DataSchema = AresSchemaHelper.CreateSchema("Success", AresDataType.Boolean),
          Index = 0 }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = PrusaMK4SCommandType.Home.ToString(),
        Description = "A command that homes the printer on the X, Y and Z axes."
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = PrusaMK4SCommandType.MoveToLastPrint.ToString(),
        Description = "A command that attempts to move the print head to be positioned above the previous print location. Designed for use with Smart Print mode ONLY. " +
        "Optional X and Y offsets can also be provided to account for the location of your camera.",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 1,
            Name = PrusaMK4SCommandParameter.Z.ToString(),
            Unit = "Coordinate",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false),
            NotPlannable = true
          },
          new ParameterMetadata
          {
            Index = 2,
            Name = PrusaMK4SCommandParameter.DwellTime.ToString(),
            Unit = "Seconds",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true),
            NotPlannable = true
          },
          new ParameterMetadata
          {
            Index = 3,
            Name = PrusaMK4SCommandParameter.XOffset.ToString(),
            Unit = "Millimeters",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true),
            NotPlannable = true
          },
          new ParameterMetadata
          {
            Index = 4,
            Name = PrusaMK4SCommandParameter.YOffset.ToString(),
            Unit = "Millimeters",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true),
            NotPlannable = true
          }
        }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = PrusaMK4SCommandType.Move.ToString(),
        Description = "A command that tells the print head to move to a specific location with an optional dwell time value. If no dwell is desired, enter zero.",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 0,
            Name = PrusaMK4SCommandParameter.X.ToString(),
            Unit = "Millimeters",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false),
            NotPlannable = true
          },
          new ParameterMetadata
          {
            Index = 1,
            Name = PrusaMK4SCommandParameter.Y.ToString(),
            Unit = "Millimeters",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false),
            NotPlannable = true
          },
          new ParameterMetadata
          {
            Index = 2,
            Name = PrusaMK4SCommandParameter.Z.ToString(),
            Unit = "Millimeters",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false),
            NotPlannable = true
          },
          new ParameterMetadata
          {
            Index = 3,
            Name = PrusaMK4SCommandParameter.DwellTime.ToString(),
            Unit = "Seconds",
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true),
            NotPlannable = true
          },
        }
      }
    };
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(PrusaMK4SCommandType deviceCommand, 
    Parameter[] parameters, 
    CommandMetadata metadata, 
    CancellationToken cancellationToken)
  {
    var result = new CommandResult();

    switch(deviceCommand)
    {
      case PrusaMK4SCommandType.Print:
      {
        var printParamsValidationResult = ValidatePrintParameters(parameters, 
          out var gcode, 
          out var nozzleTemperature, 
          out var bedTemperature,
          out var extrusionMod, 
          out var speedMod,
          out var retractionLength, 
          out var accelerationMod,
          out var fanSpeedMod);

        if(!printParamsValidationResult.Success)
        {
          result = printParamsValidationResult;
          break;
        }

        var print = await Device.Print(gcode, nozzleTemperature, bedTemperature, extrusionMod, speedMod, retractionLength, accelerationMod, fanSpeedMod);
        result.Success = print.Success;
        result.Error = print.ErrorString ?? string.Empty;
        break;
      }

      case PrusaMK4SCommandType.MoveToLastPrint:
        var zParam = parameters.FirstOrDefault(p => p.Metadata.Name == PrusaMK4SCommandParameter.Z.ToString());
        var dwellParam = parameters.FirstOrDefault(p => p.Metadata.Name == PrusaMK4SCommandParameter.DwellTime.ToString());
        var xOffsetParam = parameters.FirstOrDefault(p => p.Metadata.Name == PrusaMK4SCommandParameter.XOffset.ToString());
        var yOffsetParam = parameters.FirstOrDefault(p => p.Metadata.Name == PrusaMK4SCommandParameter.YOffset.ToString());

        if(zParam is null)
        {
          result.Success = false;
          result.Error = "Required Parameter Z or Dwell Time was null!";
          return result;
        }

        var xOffset = xOffsetParam?.Value.NumberValue ?? 0;
        var yOffset = yOffsetParam?.Value.NumberValue ?? 0;
        var dwellInt = dwellParam?.Value.NumberValue ?? 0;
        var zInt = zParam?.Value.NumberValue ?? -1;

        var smartMove = await Device.MoveToLastPrint((int)zInt, (int)dwellInt, (int)xOffset, (int)yOffset);

        result.Success = smartMove.Success;
        return result;

      case PrusaMK4SCommandType.Home:
        var home = await Device.HomePrinter();
        result.Success = home.Success;
        result.Error = home.ErrorString ?? string.Empty;
        break;

      case PrusaMK4SCommandType.GetBedTemperature:
        var bedResponse = await Device.GetAndUpdateState();
        result.Result = AresStructHelper.CreateNumberStruct("BedTemp", bedResponse.BedTemp);
        result.Success = true;
        break;

      case PrusaMK4SCommandType.GetNozzleTemperature:
        var nozzleResponse = await Device.GetAndUpdateState();
        result.Result = AresStructHelper.CreateNumberStruct("NozzleTemp", nozzleResponse.NozzleTemp);
        result.Success = true;
        break;

      case PrusaMK4SCommandType.Move:
        var movementParamsValidationResult = ValidateMoveParameters(parameters, out var x, out var y, out var z, out var dwell);

        if(!movementParamsValidationResult.Success)
        {
          result = movementParamsValidationResult;
          break;
        }

        var move = await Device.MovePrinter((int)x, (int)y, (int)z, dwell);
        result.Success = move.Success;
        result.Error = move.ErrorString ?? string.Empty;
        break;
    }

    return result;
  }

  private CommandResult ValidatePrintParameters(Parameter[] parameters, out byte[] gcode,
    out int nozzleTemperature, out int bedTemperature, out double extrusionMod, out double speedMod, out int retractionLength, out double accelerationMod, out double fanSpeedMod)
  {
    var result = new CommandResult();
    result.Success = true;

    //Assign defaults to out variables
    gcode = Array.Empty<byte>();
    nozzleTemperature = int.MinValue;
    bedTemperature = int.MinValue;
    extrusionMod = double.MinValue;
    speedMod = double.MinValue;
    retractionLength = int.MinValue;
    accelerationMod = double.MinValue;
    fanSpeedMod = double.MinValue;

    var gcodeParameter = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.GCode}"));
    var nozzleTempParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.NozzleTemperature}"));
    var bedTempParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.BedTemperature}"));
    var extrusionModParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.ExtrusionRateMod}"));
    var speedModParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.SpeedMod}"));
    var retractionLengthParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.RetractionLength}"));
    var accelerationModParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.AccelerationMod}"));
    var fanSpeedModParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.FanSpeedMod}"));

    if(gcodeParameter is null || nozzleTempParam is null || bedTempParam is null || extrusionModParam is null
      || speedModParam is null || retractionLengthParam is null || accelerationModParam is null || fanSpeedModParam is null)
    {
      result.Success = false;
      result.Error = "Not all command parameters were present. Cannot execute print command!";
      return result;
    }

    if(!gcodeParameter.Value.HasBytesValue)
    {
      result.Success = false;
      result.Error = "Failed to unpack parameter for print command!";
      return result;
    }

    if(!bedTempParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Bed Temp was not set!" };

    if(!nozzleTempParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Nozzle Temp was not set!" };

    if(!extrusionModParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Extrusion modification was not set!" };

    if(!speedModParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Speed modification was not set!" };

    if(!retractionLengthParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Retraction length was not set!" };

    if(!accelerationModParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Acceleration modification was not set!" };

    if(!fanSpeedModParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Fan Speed was not set!" };

    gcode = gcodeParameter.Value.BytesValue.ToArray();
    nozzleTemperature = (int)nozzleTempParam.Value.NumberValue;
    bedTemperature = (int)bedTempParam.Value.NumberValue;
    accelerationMod = accelerationModParam.Value.NumberValue;
    speedMod = speedModParam.Value.NumberValue;
    retractionLength = (int)retractionLengthParam.Value.NumberValue;
    extrusionMod = extrusionModParam.Value.NumberValue;
    fanSpeedMod = fanSpeedModParam.Value.NumberValue;

    return result;
  }

  private CommandResult ValidateMoveParameters(Parameter[] parameters, out float x_value, out float y_value, out float z_value, out int dwell_value)
  {
    var result = new CommandResult();
    result.Success = true;

    x_value = float.MinValue;
    y_value = float.MinValue;
    z_value = float.MinValue;
    dwell_value = int.MinValue;

    var xParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.X}"));
    var yParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.Y}"));
    var zParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.Z}"));
    var dwellParam = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{PrusaMK4SCommandParameter.DwellTime}"));

    if(xParam is null || yParam is null || zParam is null)
    {
      result.Success = false;
      result.Error = "Movement command requires X, Y and Z values. Could not complete movement request.";
      return result;
    }

    if(dwellParam is not null)
    {
      if(!dwellParam.Value.HasNumberValue)
        return new CommandResult { Success = false, Error = "Dwell Param was not properly set!"};

      dwell_value = (int)dwellParam.Value.NumberValue;
    }

    else
    {
      dwell_value = -1;
    }


    if(!xParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "X value couldn't be found!"};

    if(!yParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Y value couldn't be found!" };

    if(!zParam.Value.HasNumberValue)
      return new CommandResult { Success = false, Error = "Z value couldn't be found!" };


    x_value = (int)xParam.Value.NumberValue;
    y_value = (int)yParam.Value.NumberValue;
    z_value = (int)zParam.Value.NumberValue;

    return result;
  }
}
