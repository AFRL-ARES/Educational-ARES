using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class DispensedVolumeCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public DispensedVolumeCommand(int pump) : base($"{pump} dispensed volume", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} dispensed volume"))
  {
  }
}
