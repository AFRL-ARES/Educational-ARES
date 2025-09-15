using AresService.Services.Authentication;
using AresService.Services.Devices;
using AresService.Services.OperationalState;
using AresService.Services.UserManagement;
using EducationalAresService.Services.Devices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AresService;

public static class ServiceMapper
{
  public static void MapAresServices(this IEndpointRouteBuilder routeBuilder)
  {
    routeBuilder.MapGrpcService<AuthenticationService>();
    routeBuilder.MapGrpcService<UserManagementService>();
    routeBuilder.MapGrpcService<DeviceStateExportService>();

    //Devices
    routeBuilder.MapGrpcService<PrusaMK4SPrinterService>();
    routeBuilder.MapGrpcService<AresCameraService>();
  }
}
