namespace ChemyxPumpPlugin.Commands.Responses;

public class SetTimeResponse : ChemyxPumpResponse
{
  public SetTimeResponse(string commandEcho, string[] responseLines, string raw, double? rate, double? time) : base(commandEcho, responseLines, raw)
  {
    Rate = rate;
    Time = time;
  }

  public double? Rate { get; }
  public double? Time { get; }
}
