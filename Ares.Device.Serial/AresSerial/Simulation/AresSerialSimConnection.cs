using Ares.Device.Serial.Commands;

namespace Ares.Device.Serial.Simulation;

public abstract class AresSerialSimConnection : AresSerialConnection
{
  protected AresSerialSimConnection(SerialPortConnectionInfo connectionInfo, string deviceName, SerialConnectionOptions? connectionOptions = null) : base(connectionInfo, deviceName, connectionOptions)
  {
  }

  protected override void Open(string deviceName)
  {
    IsOpen = true;
  }

  public abstract void SendInternally(byte[] bytes);

  protected override void SendOutboundMessage(SerialCommand command)
  {
    SendInternally(command.SerializedData);
  }

  protected override void CloseCore()
  {
    IsOpen = false;
  }
}
