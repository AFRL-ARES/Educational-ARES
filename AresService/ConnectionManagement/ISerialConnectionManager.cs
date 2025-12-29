using System.Threading.Tasks;
using Ares.Device;

namespace AresService.ConnectionManagement;

public interface ISerialConnectionManager<TConnection> where TConnection : IAresDeviceConnection
{
  TConnection GetConnection(string portName, bool simulated = false);
  Task RemoveConnection(string portName, bool simulated = false);
  Task RemoveConnection(TConnection connection);
}
