using Ares.Core.Device;

namespace Ares.Core.Execution.Safety;

public class ExecutionSafetyManager : IExecutionSafetyManager
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  
  public ExecutionSafetyManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public async Task EnterSafeMode()
  {
    var deviceInterpreters = _deviceCommandInterpreterRepo.GetEnumerator();
    while(deviceInterpreters.MoveNext())
    {
      var device = deviceInterpreters.Current.Device;
      await device.EnterSafeMode();
    }
  }
}
