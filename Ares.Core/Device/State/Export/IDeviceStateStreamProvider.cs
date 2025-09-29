using Ares.Datamodel.Device;

namespace Ares.Core.Device.State.Export;

public interface IDeviceStateStreamProvider
{
  Task<IEnumerable<DeviceStateStream>> GetStream(DeviceStateRequestFilter request);
}
