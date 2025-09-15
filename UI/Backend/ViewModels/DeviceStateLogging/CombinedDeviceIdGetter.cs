namespace UI.Backend.ViewModels.DeviceStateLogging;

public class CombinedDeviceIdGetter : ICombinedDeviceIdGetter
{

  public CombinedDeviceIdGetter()
  {
  }

  public async Task<IEnumerable<string>> GetAvailableIds()
  {
    var deviceIds = new List<string>();

    return deviceIds;
  }
}
