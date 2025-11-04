using Ares.Device;

namespace AresService.ConnectionManagement;

public interface ISerialConnectionManager<TConnection> where TConnection : IAresDeviceConnection
{
  TConnection GetConnection(string portName, bool simulated = false);
  void RemoveConnection(string portName, bool simulated = false);
  void RemoveConnection(TConnection connection);
}
