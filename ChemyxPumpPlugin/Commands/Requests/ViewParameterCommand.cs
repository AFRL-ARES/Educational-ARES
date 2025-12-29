using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal class ViewParameterCommand : ChemyxPumpCommandBase<ViewParametersResponse>
{
  public ViewParameterCommand() : base("view parameter", new ViewParametersResponseParser("view parameter"))
  {
  }
}
