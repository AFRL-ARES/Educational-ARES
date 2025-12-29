using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class SetDiameterCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public SetDiameterCommand(int pump, double diameter) : base($"{pump} set diameter {diameter}", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} set diameter {diameter}"))
  {
  }
}
