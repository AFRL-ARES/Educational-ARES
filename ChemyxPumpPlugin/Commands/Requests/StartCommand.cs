using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class StartCommand : ChemyxPumpCommandBase<ChemyxPumpResponse>
{
  public StartCommand(int? pump, int mode) : base(BuildCommand(pump, $"start {mode}"), new ChemyxPumpResponseParser(BuildCommand(pump, $"start {mode}")))
  {
  }

  private static string BuildCommand(int? pump, string command)
    => pump.HasValue ? $"{pump.Value} {command}" : command;
}
