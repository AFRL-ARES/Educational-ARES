using MK4S.Services;
using PrusaMK4S.Commands.Responses;

namespace PrusaMK4S.Extensions;

public static class StatusResponseExtensions
{
  public static PrintTempsResponse ToProto(this StatusResponse status)
  {
    var response = new PrintTempsResponse();

    response.BedTemp = status.Temperature.Bed.Actual;
    response.NozzleTemp = status.Temperature.Tool.Actual;
    return response;
  }
}
