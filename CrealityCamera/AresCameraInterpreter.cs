using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Templates;
using Ares.Device;
using AresCamera.Enums;
using AresCamera.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace AresCamera;

public class AresCameraInterpreter : DeviceCommandInterpreter<IAresCamera, AresCameraCommandType>
{
  public AresCameraInterpreter(IAresCamera camera) : base(camera)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceId = Device.UniqueId,
        Name = AresCameraCommandType.CaptureImage.ToString(),
        Description = "A command that tells the camera to capture a new image.",
        OutputMetadata = new OutputMetadata() 
        {
          Description = "Returns a byte array representing the image data.", 
          DataSchema = AresSchemaBuilder.Create("ImageData", AresDataType.ByteArray).Build(),
          Index=0
        }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = AresCameraCommandType.ChangeSourceDevice.ToString(),
        Description = "A command that updates the source device used to capture images.",
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = AresCameraCommandParameter.CameraName.ToString(), NotPlannable = true } }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = AresCameraCommandType.UpdateAvailableDevices.ToString(),
        Description = "A command that updates the devices available for providing images."
      }
    };
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(AresCameraCommandType deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    var result = new CommandResult();

    switch(deviceCommandEnum)
    {
      case AresCameraCommandType.CaptureImage:
        var imageData = await Device.CaptureImage();
        result.Success = true;
        result.Result = AresStructHelper.CreateBytesStruct("ImageData", imageData);
        break;

      case AresCameraCommandType.ChangeSourceDevice:
        var newSource = parameters.FirstOrDefault(param => param.Metadata.Name.Equals($"{AresCameraCommandParameter.CameraName}"));
        result.Success = false;

        if(newSource is null)
        {
          result.Error = "No matching parameter was provided, unable to change source device!";
          break;
        }

        if(!newSource.Value.HasStringValue)
        {
          result.Error = "Failed to unpack parameter, unable to change source device!";
        }

        result.Success = await Device.UpdateSelectedDevice(newSource.Value.StringValue);

        if(!result.Success)
          result.Error = "Error trying to switch source devices!";

        break;

      case AresCameraCommandType.UpdateAvailableDevices:
        await Device.GetAvailableDevices();
        result.Success = true;
        break;

      default:
        break;
    }

    return result;
  }
}
