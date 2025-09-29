using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Datamodel.Device;
using Ares.Services;
using Google.Protobuf;
using Grpc.Core;

namespace AresService.Services.OperationalState;

public class DeviceStateExportService : Ares.Services.DeviceStateExportService.DeviceStateExportServiceBase
{
  readonly IEnumerable<IDeviceStateExportStreamProvider> _exportProviders;
  public DeviceStateExportService(IEnumerable<IDeviceStateExportStreamProvider> exportProviders)
  {
    _exportProviders = exportProviders;
  }

  public override Task<DeviceStateResponse> GetStateExport(DeviceStateRequest request, ServerCallContext context)
  {
    return request.ExportType switch
    {
      ExportType.Unspecified => GetZippedStates(request.Filter),
      ExportType.Combined => GetCombinedStates(request.Filter),
      ExportType.Zipped => GetZippedStates(request.Filter),
      _ => throw new System.NotImplementedException(),
    };
  }

  private Task<DeviceStateResponse> GetZippedStates(DeviceStateRequestFilter filter)
  {
    var provider = _exportProviders.OfType<ZippedStatesExportStreamProvider>().First();
    return GenerateStateResponse(filter, provider);
  }

  private Task<DeviceStateResponse> GetCombinedStates(DeviceStateRequestFilter filter)
  {
    var provider = _exportProviders.OfType<CombinedDeviceStateExportStreamProvider>().First();
    return GenerateStateResponse(filter, provider);
  }

  private static async Task<DeviceStateResponse> GenerateStateResponse(DeviceStateRequestFilter filter, IDeviceStateExportStreamProvider streamProvider)
  {
    var stream = await streamProvider.Export(filter);
    var byteString = await ByteString.FromStreamAsync(stream.Stream);
    return new DeviceStateResponse() { Data = byteString };
  }
}
