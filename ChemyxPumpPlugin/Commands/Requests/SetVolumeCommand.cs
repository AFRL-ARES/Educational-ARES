using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class SetVolumeCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public SetVolumeCommand(int pump, double volume) : base($"{pump} set volume {volume}", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} set volume {volume}"))
  {
  }
}
