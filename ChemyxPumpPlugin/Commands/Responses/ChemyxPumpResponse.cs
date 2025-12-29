using Ares.Device.Serial.Commands;

namespace ChemyxPumpPlugin.Commands.Responses;

public class ChemyxPumpResponse : SerialResponse
{
  public ChemyxPumpResponse(string commandEcho, string[] responseLines, string raw)
  {
    CommandEcho = commandEcho;
    ResponseLines = responseLines;
    Raw = raw;
  }

  public string CommandEcho { get; }
  public string[] ResponseLines { get; }
  public string Raw { get; }
}
