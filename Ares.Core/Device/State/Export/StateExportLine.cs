namespace Ares.Core.Device.State.Export;

public class StateExportLine
{
  public StateExportLine(IEnumerable<StateExportItem> exportItems, DateTime timestamp, string deviceName)
  {
    DeviceName = deviceName;
    Timestamp = timestamp;
    ExportItems = exportItems;
  }

  public IEnumerable<StateExportItem> ExportItems { get; }

  public DateTime Timestamp { get; }

  public string DeviceName { get; set; }
}
