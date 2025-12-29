using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class SetTimeCommand : ChemyxPumpCommandBase<SetTimeResponse>
{
  public SetTimeCommand(int pump, double minutes) : base($"{pump} set time {minutes}", new SetTimeResponseParser($"{pump} set time {minutes}"))
  {
  }
}
