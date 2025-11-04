using AresService.Services.Devices;
using AresService.Services.OperationalState;
using EducationalAresService.Services.Devices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AresService;

public static class ServiceMapper
{
  public static void MapAresServices(this IEndpointRouteBuilder routeBuilder)
  {
    routeBuilder.MapGrpcService<DeviceStateExportService>();

    //Devices
    routeBuilder.MapGrpcService<PrusaMK4SPrinterService>();
    routeBuilder.MapGrpcService<AresCameraService>();
  }
}
