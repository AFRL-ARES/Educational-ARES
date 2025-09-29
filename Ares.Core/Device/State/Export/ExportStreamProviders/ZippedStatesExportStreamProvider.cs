using System.IO.Compression;
using Ares.Datamodel.Device;

namespace Ares.Core.Device.State.Export.ExportStreamProviders;

public class ZippedStatesExportStreamProvider : IDeviceStateExportStreamProvider
{
  readonly IEnumerable<IDeviceStateStreamProvider> _streamProviders;
  public ZippedStatesExportStreamProvider(IEnumerable<IDeviceStateStreamProvider> streamProviders)
  {
    _streamProviders = streamProviders;
  }

  public string Name => "Zipped Multi Device Exporter";

  public async Task<ExportStateStream> Export(DeviceStateRequestFilter filter)
  {
    var stateStreamGetters = _streamProviders.Select(provider => provider.GetStream(filter)).ToArray();
    var stateStreamsCollection = await Task.WhenAll(stateStreamGetters);
    var stateStreams = stateStreamsCollection.SelectMany(collection => collection);
    var zipStream = new MemoryStream();

    using(var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
    {
      foreach(var stream in stateStreams)
      {
        stream.Stream.Seek(0, SeekOrigin.Begin);
        var file = archive.CreateEntry(stream.Name);
        using(var entryStream = file.Open())
        {
          await stream.Stream.CopyToAsync(entryStream);
          await entryStream.FlushAsync();
        }
        stream.Stream.Dispose();
      }
    }

    zipStream.Seek(0, SeekOrigin.Begin);
    return new ExportStateStream(zipStream, "zip");
  }
}
