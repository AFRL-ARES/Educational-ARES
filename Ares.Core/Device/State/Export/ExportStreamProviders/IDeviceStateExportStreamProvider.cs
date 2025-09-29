using Ares.Datamodel.Device;

namespace Ares.Core.Device.State.Export.ExportStreamProviders;

/// <summary>
/// An interface for the final endpoint of getting a device state stream.
/// The stream that's provided by this interface should be ready to be written
/// </summary>
public interface IDeviceStateExportStreamProvider
{
  /// <summary>
  /// Provides a byte stream all the device states that are requested based on a given state request
  /// </summary>
  /// <param name="request"></param>
  ///   ex.: interval of 1 second, would export the state of the requested device
  ///         at every second from the given start time to the end time.
  ///         If start/end times are not given, the first and last timestamped state will be used</param>
  /// <returns>A stream that can be written/downloaded/etc. Not guaranteed to be at position 0.</returns>
  public Task<ExportStateStream> Export(DeviceStateRequestFilter request);


  /// <summary>
  /// The name of the exporter that can be presented to the user if they had multiple exporters to pick from
  /// </summary>
  public string Name { get; }
}
