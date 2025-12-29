using System.Globalization;
using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Parsing;

namespace ChemyxPumpPlugin.Commands.Responses;

internal class NumericResponseParser<TResponse> : SerialResponseParser<TResponse> where TResponse : NumericResponse
{
  private readonly Func<string, string[], string, double?, TResponse> _factory;
  private readonly string _originalCommand;

  public NumericResponseParser(Func<string, string[], string, double?, TResponse> factory, string originalCommand)
  {
    _factory = factory;
    _originalCommand = originalCommand;
  }

  public override bool TryParseResponse(byte[] buffer, out TResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    response = null;
    if(!ChemyxPumpParsing.TryParse(buffer, _originalCommand, out var baseResponse, out dataToRemove))
      return false;

    var line = baseResponse.ResponseLines.FirstOrDefault();
    double? parsed = null;
    if(!string.IsNullOrWhiteSpace(line))
    {
      var lastPart = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
      if(double.TryParse(lastPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        parsed = value;
    }

    response = _factory(baseResponse.CommandEcho, baseResponse.ResponseLines, baseResponse.Raw, parsed);
    return true;
  }
}
