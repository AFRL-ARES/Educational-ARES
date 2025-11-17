using System.Collections.Generic;
using Ares.Device;

namespace AresService.ConnectionManagement;

public class SerialConnectionRepository : List<IAresDeviceConnection>, ISerialConnectionRepository
{
}
