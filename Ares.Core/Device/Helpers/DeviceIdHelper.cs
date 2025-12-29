namespace Ares.Core.Device.Helpers;
using Microsoft.Extensions.Logging;

public class DeviceIdHelper(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo, ILogger<DeviceIdHelper> logger)
{
  public string DeviceIdToName(string id)
  {
    var device = deviceCommandInterpreterRepo.FirstOrDefault(dci => dci.Device.UniqueId == id);
    if (device != null) 
      return device.Device.Name;
    
    logger.LogDebug($"Device was not found in the interpreter repo with id of {id}");
    logger.LogDebug($"The following devices are found in the repo");
    foreach(var interpreter in deviceCommandInterpreterRepo)
    {
      logger.LogDebug("{Id} - {Name}", interpreter.Device?.UniqueId, interpreter.Device?.Name);
    }

    return "";
  }
}
