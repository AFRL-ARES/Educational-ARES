using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class SetDelayCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public SetDelayCommand(int pump, double delay) : base($"{pump} set delay {delay}", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} set delay {delay}"))
  {
  }
}
