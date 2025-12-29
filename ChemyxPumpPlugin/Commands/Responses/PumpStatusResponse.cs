namespace ChemyxPumpPlugin.Commands.Responses;

public class PumpStatusResponse : ChemyxPumpResponse
{
  public PumpStatusResponse(string commandEcho, string[] responseLines, string raw, int? status) : base(commandEcho, responseLines, raw)
  {
    Status = (PumpStatus?)status;
  }

  public PumpStatus? Status { get; }
}
