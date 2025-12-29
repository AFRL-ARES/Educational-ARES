namespace ChemyxPumpPlugin.Commands.Responses;

public class LimitParameterResponse : ChemyxPumpResponse
{
  public LimitParameterResponse(string commandEcho, string[] responseLines, string raw, double? maxRate, double? minRate, double? maxVolume, double? minVolume) : base(commandEcho, responseLines, raw)
  {
    MaxRate = maxRate ?? -1.0;
    MinRate = minRate ?? -1.0;
    MaxVolume = maxVolume ?? -1.0;
    MinVolume = minVolume ?? -1.0;
  }

  public double MaxRate { get; }
  public double MinRate { get; }
  public double MaxVolume { get; }
  public double MinVolume { get; }
}
