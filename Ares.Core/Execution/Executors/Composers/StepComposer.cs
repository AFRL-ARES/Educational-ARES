using Ares.Core.Device;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class StepComposer : ICommandComposer<StepTemplate, StepExecutor>
{
  private readonly IDeviceCommandInterpreterRepo _interpreterRepo;

  public StepComposer(IDeviceCommandInterpreterRepo interpreterRepo)
  {
    _interpreterRepo = interpreterRepo;
  }

  public StepExecutor Compose(StepTemplate template)
  { 
    var executables =
      template
        .CommandTemplates
        .OrderBy(t => t.Index)
        .Select
        (
          commandTemplate =>
          {
            var deviceId = commandTemplate.Metadata?.DeviceId;
            var commandInterpreter =
              _interpreterRepo
                .First(interpreter =>
                {
                  var device = interpreter.Device;
                  // Prefer match by UniqueId; fall back to Name. Guard against nulls.
                  return (deviceId is not null && string.Equals(device.UniqueId, deviceId, StringComparison.Ordinal))
                         || (deviceId is not null && string.Equals(device.Name, deviceId, StringComparison.Ordinal));
                });

            var command = commandInterpreter.TemplateToDeviceCommand(commandTemplate);
            var executable = new CommandExecutor(command, commandTemplate);
            return executable;
          }
        )
        .ToArray();


    return template.IsParallel
      ? new ParallelStepExecutor(template, executables)
      : new SequentialStepExecutor(template, executables);
  }

  public StepExecutor Compose(StepTemplate template, ExperimentExecutionStatus experimentExecutionStatus)
  {
    throw new NotImplementedException();
  }
}
