using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class PumpStatusCommand : ChemyxPumpCommandBase<PumpStatusResponse>
{
  public PumpStatusCommand(int pump) : base($"{pump} pump status", new PumpStatusResponseParser($"{pump} pump status"))
  {
  }
}
