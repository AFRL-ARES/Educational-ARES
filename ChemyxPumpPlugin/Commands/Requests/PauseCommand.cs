using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class PauseCommand : ChemyxPumpCommandBase<ChemyxPumpResponse>
{
  public PauseCommand(int? pump) : base(BuildCommand(pump, "pause"), new ChemyxPumpResponseParser(BuildCommand(pump, "pause")))
  {
  }

  private static string BuildCommand(int? pump, string command)
    => pump.HasValue ? $"{pump.Value} {command}" : command;
}
