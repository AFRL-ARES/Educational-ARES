using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class ChangeRateCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public ChangeRateCommand(int pump, double rate) : base($"{pump} change rate {rate}", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} change rate {rate}"))
  {
  }
}
