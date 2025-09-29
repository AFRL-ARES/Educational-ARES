namespace Ares.Core.Device.State.Export;

/// <summary>
/// This class contains the data needed to export the state of a single device
/// </summary>
public class SingleDeviceStateExportData
{
  public SingleDeviceStateExportData(string deviceName, StateExportLine[] exportLines)
  {
    DeviceName = deviceName;
    ExportLines = exportLines;
  }

  public string DeviceName { get; set; }

  /// <summary>
  /// The lines are not guaranteed to be ordered
  /// </summary>
  public StateExportLine[] ExportLines { get; }
}
