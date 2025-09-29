using Ares.Device;

namespace Ares.Core.Device.State.Logging;

public abstract class DeviceStateLoggerFactory<TDevice> : IDeviceStateLoggerFactory
  where TDevice : IAresDevice
{
  public bool CanHandle(IAresDevice device) => device is TDevice;
  public IDeviceStateLogger Create(IAresDevice device) => Create((TDevice)device);

  protected abstract IDeviceStateLogger Create(TDevice device);
}