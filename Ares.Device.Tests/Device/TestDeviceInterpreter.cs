using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Templates;

namespace Ares.Device.Tests.Device;

public class TestDeviceInterpreter : DeviceCommandInterpreter<TestDevice, TestDeviceCommand>
{
  public TestDeviceInterpreter(TestDevice device) : base(device)
  {
  }

  protected override Task<CommandResult> ParseAndPerformDeviceAction(TestDeviceCommand deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    switch(deviceCommandEnum)
    {
      case TestDeviceCommand.Record:
      case TestDeviceCommand.Record2:
      case TestDeviceCommand.Record3:
        var result = new CommandResult();
        var param = parameters.First(parameter => parameter.Metadata.Name == TestDeviceCommandParameter.ReplyParameter.ToString());
        
        if(!param.Value.HasNumberValue)
        {
          result.Success = false;
          result.Error = "Test Device expected a number as it's parameter, but none was received!";
          return Task.FromResult(result);
        }

        result.Result = AresStructHelper.CreateNumberStruct("Test", param.Value.NumberValue);
        result.Success = true;
        result.UniqueId = Guid.NewGuid().ToString();
        return Task.FromResult(result);
      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    // return in opposite order to test the ordering.
    return new[] { GetTestMetadata(2), GetTestMetadata(1), GetTestMetadata(0) };
  }

  private CommandMetadata GetTestMetadata(int idx)
  {
    var testCommandMetadata = new CommandMetadata
    {
      Description = "Test command that takes in an int and returns an int",
      DeviceId = Device.UniqueId,
      Name = ((TestDeviceCommand)idx).ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      OutputMetadata = new OutputMetadata
      {
        UniqueId = Guid.NewGuid().ToString(),
        DataSchema = AresSchemaBuilder.Create("TestDeviceInterpreter", AresDataType.Number).Build(),
        Description = "A test response for the test command",
        Index = idx
      }
    };

    testCommandMetadata.ParameterMetadatas.Add(new ParameterMetadata
    {
      Name = TestDeviceCommandParameter.ReplyParameter3.ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "TestUnit",
      Index = 2
    });

    testCommandMetadata.ParameterMetadatas.Add(new ParameterMetadata
    {
      Name = TestDeviceCommandParameter.ReplyParameter2.ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "TestUnit",
      Index = 1
    });

    testCommandMetadata.ParameterMetadatas.Add(new ParameterMetadata
    {
      Name = TestDeviceCommandParameter.ReplyParameter.ToString(),
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "TestUnit",
      Index = 0
    });

    return testCommandMetadata;
  }
}
