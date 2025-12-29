using Ares.Device.Serial.Commands;

namespace ChemyxPumpPlugin.Commands.Responses;

public class ViewParametersResponse : SerialResponse
{
  public ViewParametersResponse(SinglePumpParameters[] parameters)
  {
    PumpParameters = parameters;
  }

  public SinglePumpParameters[] PumpParameters { get; private set; }
}
