using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class SetRateCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public SetRateCommand(int pump, double rate) : base($"{pump} set rate {rate}", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} set rate {rate}"))
  {
  }
}
