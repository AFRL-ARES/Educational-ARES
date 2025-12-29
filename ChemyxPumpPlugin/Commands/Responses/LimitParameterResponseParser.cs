using System.Globalization;
using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Parsing;

namespace ChemyxPumpPlugin.Commands.Responses;

internal class LimitParameterResponseParser : SerialResponseParser<LimitParameterResponse>
{
  private readonly string _originalCommand;

  public LimitParameterResponseParser(string originalCommand)
  {
    _originalCommand = originalCommand;
  }

  public override bool TryParseResponse(byte[] buffer, out LimitParameterResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    response = null;
    if(!ChemyxPumpParsing.TryParse(buffer, _originalCommand, out var baseResponse, out dataToRemove))
      return false;

    var line = baseResponse.ResponseLines.FirstOrDefault();
    var parts = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    double?[] values = new double?[4];
    for(var i = 0; i < Math.Min(parts.Length, 4); i++)
    {
      if(double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        values[i] = v;
    }

    response = new LimitParameterResponse(
      baseResponse.CommandEcho,
      baseResponse.ResponseLines,
      baseResponse.Raw,
      values.ElementAtOrDefault(0),
      values.ElementAtOrDefault(1),
      values.ElementAtOrDefault(2),
      values.ElementAtOrDefault(3));

    return true;
  }
}
