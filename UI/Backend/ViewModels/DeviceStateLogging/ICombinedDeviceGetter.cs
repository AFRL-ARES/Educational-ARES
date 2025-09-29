using Ares.Messages.DeviceStates;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public interface ICombinedDeviceGetter
{
  Task<DevicesDescription[]> GetAvailableDevices();
}
