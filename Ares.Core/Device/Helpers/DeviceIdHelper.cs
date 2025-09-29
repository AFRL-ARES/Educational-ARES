namespace Ares.Core.Device.Helpers;

public class DeviceIdHelper(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
{
  public string DeviceIdToName(string id)
  {
    var device = deviceCommandInterpreterRepo.First(dci => dci.Device.UniqueId == id);
    return device.Device.Name;
  }
}