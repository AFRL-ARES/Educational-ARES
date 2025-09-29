using Ares.Messages.DeviceStates;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class CombinedDeviceGetter : ICombinedDeviceGetter
{
  public CombinedDeviceGetter()
  {
    //TODO: Add logging for printer?
  }

  public Task<DevicesDescription[]> GetAvailableDevices()
  {
    var deviceIds = new List<DevicesDescription>().ToArray();

    return Task.FromResult(deviceIds);
  }
}
