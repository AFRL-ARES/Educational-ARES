using System.Globalization;
using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Parsing;

namespace ChemyxPumpPlugin.Commands.Responses;

internal class PumpStatusResponseParser : SerialResponseParser<PumpStatusResponse>
{
  private readonly string _originalCommand;

  public PumpStatusResponseParser(string originalCommand)
  {
    _originalCommand = originalCommand;
  }
  public override bool TryParseResponse(byte[] buffer, out PumpStatusResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    response = null;
    if(!ChemyxPumpParsing.TryParse(buffer, _originalCommand, out var baseResponse, out dataToRemove))
      return false;

    int? status = null;
    var line = baseResponse.ResponseLines.FirstOrDefault();
    if(!string.IsNullOrWhiteSpace(line) && int.TryParse(line.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
      status = s;

    response = new PumpStatusResponse(baseResponse.CommandEcho, baseResponse.ResponseLines, baseResponse.Raw, status);
    return true;
  }
}
