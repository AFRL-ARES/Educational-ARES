using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class StopCommand : ChemyxPumpCommandBase<ChemyxPumpResponse>
{
  public StopCommand(int? pump) : base(BuildCommand(pump, "stop"), new ChemyxPumpResponseParser(BuildCommand(pump, "stop")))
  {
  }

  private static string BuildCommand(int? pump, string command)
    => pump.HasValue ? $"{pump.Value} {command}" : command;
}
