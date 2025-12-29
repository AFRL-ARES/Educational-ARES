using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class ElapsedTimeCommand : ChemyxPumpCommandBase<NumericResponse>
{
  public ElapsedTimeCommand(int pump) : base($"{pump} elapsed time", new NumericResponseParser<NumericResponse>((c, l, r, v) => new NumericResponse(c, l, r, v), $"{pump} elapsed time"))
  {
  }
}
