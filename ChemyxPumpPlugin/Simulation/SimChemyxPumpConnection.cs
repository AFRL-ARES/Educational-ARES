using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;
using System.Text;

namespace ChemyxPumpPlugin.Simulation;

public class SimChemyxPumpConnection : AresSerialSimConnection, IChemyxPumpConnection
{
  private readonly SimChemyxPump _pump;

  public SimChemyxPumpConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(150) })
  {
    _pump = new SimChemyxPump(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _pump.SendCommand(bytes);
  }

  public static string ToPrintableUtf8(byte[] bytes)
  {
    string text = Encoding.UTF8.GetString(bytes);
    var sb = new StringBuilder();

    foreach(char c in text)
    {
      // Use Unicode categories to detect control chars
      if(char.IsControl(c))
      {
        sb.Append($"\\u{((int)c):X4}");
      }
      else
      {
        sb.Append(c);
      }
    }

    return sb.ToString();
  }
}
