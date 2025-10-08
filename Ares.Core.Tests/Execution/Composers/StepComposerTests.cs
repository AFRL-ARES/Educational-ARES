using Ares.Core.Device;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Device;
using Moq;
using System.Reflection;
using Ares.Datamodel.Templates;
using Ares.Core.Notifications;

namespace Ares.Core.Tests.Execution.Composers;

internal class StepComposerTests
{
  private IDeviceCommandInterpreterRepo _commandInterpreters;
  private IEnumerable<INotificationHandler> _notificationHandlers;
  private StepTemplate _stepTemplate;

  [SetUp]
  public void SetUp()
  {
    var commandTemplate1 = new CommandTemplate
    {
      Index = 0,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestName" }
    };

    var commandTemplate2 = new CommandTemplate
    {
      Index = 1,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestName" }
    };

    var commandTemplate3 = new CommandTemplate
    {
      Index = 2,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestName" }
    };

    var commandTemplate4 = new CommandTemplate
    {
      Index = 3,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestName" }
    };

    var stepTemplate = new StepTemplate
    { Index = 0, UniqueId = Guid.NewGuid().ToString() };

    stepTemplate.CommandTemplates.Add(commandTemplate3);
    stepTemplate.CommandTemplates.Add(commandTemplate1);
    stepTemplate.CommandTemplates.Add(commandTemplate4);
    stepTemplate.CommandTemplates.Add(commandTemplate2);

    _stepTemplate = stepTemplate;
  }

  [OneTimeSetUp]
  public void OneTimeSetUp()
  {
    var interpreterMock = new Mock<IDeviceCommandInterpreter<IAresDevice>>();
    interpreterMock.SetupGet(interpreter => interpreter.Device.Name).Returns("TestName");
    _commandInterpreters = new DeviceCommandInterpreterRepo()
    {
      interpreterMock.Object
    };

    _notificationHandlers = new Mock<IEnumerable<INotificationHandler>>().Object;
  }

  [Test]
  public void StepComposer_Composes_CommandTemplates_Correctly()
  {
    var stepComposer = new StepComposer(_commandInterpreters, _notificationHandlers);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    var templates = stepExecutor.CommandExecutors.Select(executor => typeof(CommandExecutor).GetProperty("Template", BindingFlags.Public | BindingFlags.Instance).GetValue(executor)).OfType<CommandTemplate>();
    Assert.That(templates.Select((template, i) => template.Index == i), Is.All.True);
  }

  [Test]
  public void StepComposer_Composes_Parallel_Template()
  {
    _stepTemplate.IsParallel = true;
    var stepComposer = new StepComposer(_commandInterpreters, _notificationHandlers);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    Assert.That(stepExecutor, Is.TypeOf<ParallelStepExecutor>());
  }

  [Test]
  public void StepComposer_Composes_Sequential_Template()
  {
    _stepTemplate.IsParallel = false;
    var stepComposer = new StepComposer(_commandInterpreters, _notificationHandlers);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    Assert.That(stepExecutor, Is.TypeOf<SequentialStepExecutor>());
  }
}
