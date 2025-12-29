namespace ChemyxPumpPlugin.Commands.Responses;

public class NumericResponse : ChemyxPumpResponse
{
  public NumericResponse(string commandEcho, string[] responseLines, string raw, double? value) : base(commandEcho, responseLines, raw)
  {
    Value = value;
  }

  public double? Value { get; }
}
