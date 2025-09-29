namespace Ares.Core.Device.State.Export;

/// <summary>
/// Holds data for a single state property of a device ex.: for an MFC this could be: name = "Temperature", source =
/// "MFC1", value = 24, timestamp = 1/1/1990,
/// </summary>
public class StateExportItem
{
  public StateExportItem(string name, string source, object? value)
  {
    Name = name;
    Source = source;
    Value = value;
  }

  public string Name { get; }

  public string Source { get; }

  public object? Value { get; }
}
