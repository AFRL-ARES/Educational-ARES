using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Parsing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ChemyxPumpPlugin.Commands.Responses;

public partial class ViewParametersResponseParser : SerialResponseParser<ViewParametersResponse>
{
  private readonly string _originalCommand;

  public ViewParametersResponseParser(string originalCommand)
  {
    _originalCommand = originalCommand;
  }

  public override bool TryParseResponse(byte[] buffer, out ViewParametersResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var basicParse = ChemyxPumpParsing.TryParse(buffer, _originalCommand, out var genericResponse, out dataToRemove);

    if(!basicParse)
    {
      response = null;
      return false;
    }

    var lines = genericResponse.ResponseLines.Skip(1);

    if(lines.First().StartsWith("pump", StringComparison.OrdinalIgnoreCase))
    {
      var split = SplitByPump(genericResponse.ResponseLines).ToArray();
      var multiParameters = split.Select(ParseParametersForPump).ToArray();
      response = new ViewParametersResponse(multiParameters);
      return true;
    }
    else
    {
      // We're gonna assume that single pump pumps do not preface their list of params with "Pump #"
      var parameters = ParseParametersForPump(genericResponse.ResponseLines);
      response = new ViewParametersResponse([parameters]);
      return true;
    }
  }

  private SinglePumpParameters ParseParametersForPump(string[] lines)
  {
    var unit = PumpUnits.MillilitersPerMinute;
    var diameter = 0.0;
    var rate = 0.0;
    var time = TimeSpan.FromMinutes(0);
    var volume = 0.0;
    var delay = TimeSpan.FromMinutes(0);

    foreach(var line in lines)
    {
      var l = line.ToLower();
      var stringVal = GetRelevantSubstring(l);
      if(l.StartsWith("unit"))
      {
        var intVal = int.Parse(stringVal);
        unit = (PumpUnits)intVal;
      }
      else if(l.StartsWith("dia"))
      {
        diameter = double.Parse(stringVal);
      }
      else if(l.StartsWith("rate"))
      {
        rate = double.Parse(stringVal);
      }
      else if(l.StartsWith("time"))
      {
        var minutes = double.Parse(stringVal);
        time = TimeSpan.FromMinutes(minutes);
      }
      else if(l.StartsWith("volume"))
      {
        volume = double.Parse(stringVal);
      }
      else if (l.StartsWith("delay"))
      {
        var seconds = double.Parse(stringVal);
        delay = TimeSpan.FromSeconds(seconds);
      }
    }

    return new SinglePumpParameters(unit, diameter, rate, time, volume, delay);
  }

  private string GetRelevantSubstring(string pumpParamString)
  {
    var regex = PumpParamRegex();
    var matched = regex.Match(pumpParamString);
    return matched.Groups[1].Value;
  }

  private IEnumerable<string[]> SplitByPump(IEnumerable<string> lines)
  {
    List<string>? currentGroup = null;

    foreach(var line in lines)
    {
      if(line.StartsWith("pump", StringComparison.OrdinalIgnoreCase))
      {
        if(currentGroup is not null)
          yield return currentGroup.ToArray();

        currentGroup = [];
      }
      else
      {
        currentGroup?.Add(line);
      }
    }

    if(currentGroup is not null)
      yield return currentGroup.ToArray();
  }

  [GeneratedRegex("\\w+\\s=\\s(\\S+)")]
  private static partial Regex PumpParamRegex();
}
