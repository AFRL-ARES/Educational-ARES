using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class ReadLimitParameterCommand : ChemyxPumpCommandBase<LimitParameterResponse>
{
  public ReadLimitParameterCommand(int pump, int programIndex) : base($"{pump} read limit parameter {programIndex}", new LimitParameterResponseParser($"{pump} read limit parameter {programIndex}"))
  {
  }
}
