using System.Collections.Generic;
using Ares.Device;

namespace AresService.ConnectionManagement;

public interface ISerialConnectionRepository : ICollection<IAresDeviceConnection>
{
}
