using Ares.Device.Serial;
using System.IO.Ports;

namespace ChemyxPumpPlugin;

public class ChemyxPumpConnection : AresHardwareConnection, IChemyxPumpConnection
{
  public ChemyxPumpConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One),  portName)
  {
    
  }
}
