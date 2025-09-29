using System.Globalization;
using Ares.Core.Device.Helpers;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel.Device;
using CsvHelper;
using CsvHelper.Configuration;

namespace Ares.Core.Device.Remote.State;
public class DeviceStateStreamProvider : IDeviceStateStreamProvider
{
  private readonly IDeviceStateGetter _deviceStateGetter;

  public DeviceStateStreamProvider(IDeviceStateGetter deviceStateGetter)
  {
    _deviceStateGetter = deviceStateGetter;
  }

  public async Task<IEnumerable<DeviceStateStream>> GetStream(DeviceStateRequestFilter request)
  {
    var stateMaps = await _deviceStateGetter.GetStates<DeviceState>(request);
    var exports = new List<DeviceStateStream>();

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      NewLine = Environment.NewLine
    };

    foreach(var map in stateMaps)
    {
      var stream = new MemoryStream();
      // Leave the stream open when the writer is disposed by setting leaveOpen to true
      var writer = new StreamWriter(stream, leaveOpen: true);
      var csv = new CsvWriter(writer, config);

      try
      {
        var valuePairLines = ToDictionary(map.Value);

        // Get headers from the first record, ensuring a consistent order for all rows.
        var headers = valuePairLines.FirstOrDefault()?.Keys.ToList() ?? [];

        // 1. Write the headers
        foreach(var header in headers)
        {
          csv.WriteField(header);
        }
        await csv.NextRecordAsync();

        // 2. Write the data rows
        foreach(var record in valuePairLines)
        {
          foreach(var header in headers)
          {
            record.TryGetValue(header, out var value);
            csv.WriteField(value ?? string.Empty);
          }
          await csv.NextRecordAsync();
        }

        await writer.FlushAsync(); // Flush the StreamWriter
        stream.Position = 0; // Reset the stream position to the beginning

        exports.Add(new DeviceStateStream(Path.ChangeExtension(map.Key, "csv"), stream));
      }
      finally
      {
        // CsvWriter does not dispose the underlying writer by default.
        // We should dispose it to ensure everything is flushed correctly.
        await writer.DisposeAsync();
      }
    }
    return exports;
  }

  private Dictionary<string, string>[] ToDictionary(IEnumerable<DeviceState> states)
  {
    var kvps = states
      .OrderBy(val => val.Timestamp)
      .Select(ToDictionary).ToArray();
    return kvps;
  }

  private Dictionary<string, string> ToDictionary(DeviceState state)
  {
    var dict = new Dictionary<string, string>
    {
      ["Timestamp"] = state.Timestamp.ToString()
    };
    foreach(var dataField in state.Data?.Fields ?? [])
    {
      dict[dataField.Key] = dataField.Value.GetValueAsString();
    }

    return dict;
  }
}

