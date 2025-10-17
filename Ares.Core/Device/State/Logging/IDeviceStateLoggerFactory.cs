using Ares.Device;

namespace Ares.Core.Device.State.Logging;

public interface IDeviceStateLoggerFactory
{
  bool CanHandle(IAresDevice device);
  IDeviceStateLogger Create(IAresDevice device);
}
