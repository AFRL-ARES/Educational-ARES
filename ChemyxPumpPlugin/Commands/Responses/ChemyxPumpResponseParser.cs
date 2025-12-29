using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Parsing;

namespace ChemyxPumpPlugin.Commands.Responses;

internal class ChemyxPumpResponseParser : SerialResponseParser<ChemyxPumpResponse>
{
  private readonly string _originalCommand;

  public ChemyxPumpResponseParser(string originalCommand)
  {
    _originalCommand = originalCommand;
  }

  public override bool TryParseResponse(byte[] buffer, out ChemyxPumpResponse? response, out ArraySegment<byte>? dataToRemove)
    => ChemyxPumpParsing.TryParse(buffer, _originalCommand, out response!, out dataToRemove);
}
