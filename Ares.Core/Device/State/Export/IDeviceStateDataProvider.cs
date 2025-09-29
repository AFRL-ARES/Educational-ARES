using Ares.Datamodel.Device;

namespace Ares.Core.Device.State.Export;

/// <summary>
/// This interface defines how to get the state data for multiple devices of the same type
/// </summary>
public interface IDeviceStateDataProvider
{
  Task<IEnumerable<SingleDeviceStateExportData>> GetExportData(DeviceStateRequestFilter request);
}
