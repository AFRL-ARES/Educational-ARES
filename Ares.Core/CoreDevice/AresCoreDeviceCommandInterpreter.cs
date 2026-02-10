using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Templates;
using Ares.Device;
using UnitsNet.Units;

namespace Ares.Core.CoreDevice;

public class AresCoreDeviceCommandInterpreter : DeviceCommandInterpreter<AresCoreDevice, AresCoreDeviceCommand>
{
  public AresCoreDeviceCommandInterpreter(AresCoreDevice device) : base(device)
  { }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return
    [
      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = AresCoreDeviceCommand.Sleep.ToString(),
        Description = "Sleep for a given amount of time.",
        ParameterMetadatas =
          {
            new ParameterMetadata
            {
              Name = AresCoreDeviceCommandParameter.Duration.ToString(),
              Index = 0,
              Unit = $"{DurationUnit.Millisecond}s",
              Schema = AresSchemaBuilder.Entry(AresDataType.Number).Build()
            }
          }
      },

      new CommandMetadata
      {
        DeviceId = Device.UniqueId,
        Name = AresCoreDeviceCommand.WaitForUser.ToString(),
        Description = "ARES will request user confirmation before continuing."
      }
    ];
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(AresCoreDeviceCommand deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    var result = new CommandResult();
    switch(deviceCommandEnum)
    {
      case AresCoreDeviceCommand.Sleep:
        var durationParam = parameters[0];

        var duration = UnitsNet.Duration.FromMilliseconds(durationParam.Value.NumberValue);
        await Device.Sleep(duration.ToTimeSpan(), cancellationToken);
        result.Success = true;
        return result;

      case AresCoreDeviceCommand.WaitForUser:
        result.Success = true;
        result.AwaitUserInput = true;
        return result;

      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }
  }
}
