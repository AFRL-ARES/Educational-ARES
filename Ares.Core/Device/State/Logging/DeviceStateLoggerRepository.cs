using System.Collections.Concurrent;

namespace Ares.Core.Device.State.Logging;
public class DeviceStateLoggerRepository : ConcurrentDictionary<string, IDeviceStateLogger>, IDeviceStateLoggerRepository
{
}
