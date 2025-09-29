using Ares.Device.Tests.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Moq;
using Moq.Protected;

namespace Ares.Device.Tests;

internal class DeviceLibraryTests
{
  [Test]
  public void Device_Interpreter_Should_Return_Properly_Ordered_Commands()
  {
    var device = new TestDevice();
    var interpreter = new TestDeviceInterpreter(device);
    var commands = interpreter.CommandsToIndexedMetadatas().ToArray();

    Assert.That(commands.All(metadata => metadata.ParameterMetadatas.Select((parameterMetadata, i) => parameterMetadata.Index == i).All(b => b)), "One or more parameters were not ordered properly.");
  }

  [Test]
  public async Task Device_Interpreter_Should_Execute_Given_Command_Template()
  {
    var device = new TestDevice();
    var interpreter = new TestDeviceInterpreter(device);
    var commands = interpreter.CommandsToIndexedMetadatas().ToArray();
    var commandMeta = commands.First(metadata => metadata.Name == TestDeviceCommand.Record.ToString());
    var command = new CommandTemplate
    {
      Metadata = commandMeta,
      UniqueId = Guid.NewGuid().ToString()
    };

    var parameter = new Parameter();
    parameter.Metadata = commandMeta.ParameterMetadatas.First(metadata => metadata.Name == TestDeviceCommandParameter.ReplyParameter.ToString());
    parameter.UniqueId = Guid.NewGuid().ToString();
    parameter.Value = AresValueHelper.CreateNumber(12345);

    command.Parameters.Add(parameter);

    var resultGetter = interpreter.TemplateToDeviceCommand(command);
    var result = await resultGetter(CancellationToken.None);

    Assert.That(result.Success);
    var num = result.Result.Fields.FirstOrDefault(field => field.Key == "Test");
    Assert.That(num.Value.NumberValue, Is.EqualTo(12345));
  }

  [Test]
  public void Interpreter_Should_Throw_Exception_When_Parameter_Indexes_Incorrect()
  {
    var device = new TestDevice();
    var interpreter = new Mock<TestDeviceInterpreter>(device);
    interpreter.Protected().Setup<CommandMetadata[]>("CommandsToMetadatas").Returns(() =>
    {
      var meta = new CommandMetadata
      {
        DeviceId = device.UniqueId,
        Name = "Test"
      };

      var paramMeta = new ParameterMetadata
      {
        Index = 1,
        Name = "Test"
      };

      var paramMeta2 = new ParameterMetadata
      {
        Index = 1,
        Name = "Test"
      };

      meta.ParameterMetadatas.Add(paramMeta);
      meta.ParameterMetadatas.Add(paramMeta2);

      return new[] { meta };
    });

    Assert.Throws<Exception>(() => interpreter.Object.CommandsToIndexedMetadatas());
  }

  [Test]
  public void Interpreter_Should_Order_Parameter_Indexes_When_Default()
  {
    var device = new TestDevice();
    var interpreter = new Mock<TestDeviceInterpreter>(device);
    var parameter1Name = "Test1";
    var parameter2Name = "Test2";
    var parameter3Name = "Test3";
    interpreter.Protected().Setup<CommandMetadata[]>("CommandsToMetadatas").Returns(() =>
    {
      var meta = new CommandMetadata
      {
        DeviceId = device.UniqueId,
        Name = "Test"
      };

      var paramMeta = new ParameterMetadata
      {
        Index = default,
        Name = parameter1Name
      };

      var paramMeta2 = new ParameterMetadata
      {
        Index = default,
        Name = parameter2Name
      };

      var paramMeta3 = new ParameterMetadata
      {
        Index = default,
        Name = parameter3Name
      };

      meta.ParameterMetadatas.Add(paramMeta);
      meta.ParameterMetadatas.Add(paramMeta2);
      meta.ParameterMetadatas.Add(paramMeta3);

      return new[] { meta };
    });

    var commandMetadata = interpreter.Object.CommandsToIndexedMetadatas().First();
    var parameterMetadata1 = commandMetadata.ParameterMetadatas.First(metadata => metadata.Index == 0);
    var parameterMetadata2 = commandMetadata.ParameterMetadatas.First(metadata => metadata.Index == 1);
    var parameterMetadata3 = commandMetadata.ParameterMetadatas.First(metadata => metadata.Index == 2);
    Assert.Multiple(() =>
    {
      Assert.That(parameterMetadata1.Name, Is.EqualTo(parameter1Name));
      Assert.That(parameterMetadata2.Name, Is.EqualTo(parameter2Name));
      Assert.That(parameterMetadata3.Name, Is.EqualTo(parameter3Name));
    });
  }
}
