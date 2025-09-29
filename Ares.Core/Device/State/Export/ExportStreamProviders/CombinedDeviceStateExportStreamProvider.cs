using System.Globalization;
using Ares.Datamodel.Device;
using CsvHelper;
using CsvHelper.Configuration;
using Google.Protobuf.Collections;

namespace Ares.Core.Device.State.Export.ExportStreamProviders;

/// <summary>
/// Creates a single csv file containing all the device states
/// </summary>
public class CombinedDeviceStateExportStreamProvider : IDeviceStateExportStreamProvider
{
  readonly IEnumerable<IDeviceStateDataProvider> _dataProviders;

  public string Name => "Single File CSV Exporter";

  public CombinedDeviceStateExportStreamProvider(IEnumerable<IDeviceStateDataProvider> dataProviders)
  {
    _dataProviders = dataProviders;
  }

  public async Task<ExportStateStream> Export(DeviceStateRequestFilter request)
  {
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      NewLine = Environment.NewLine
    };

    var exportData = await GetStateExportData(request);

    var stream = new MemoryStream();
    var writer = new StreamWriter(stream);
    using(var csv = new CsvWriter(writer, config, true))
    {
      csv.Context.TypeConverterCache.AddConverter<RepeatedField<string>>(new StringCollectionConverter());
      WriteHeader(csv, exportData);

      var lines = exportData.SelectMany(d => d.ExportLines);
      var timeOrderedLines = lines.OrderBy(l => l.Timestamp);
      //The local time offset won't change in between records 
      var localTimeOffset = DateTimeOffset.Now.Offset.ToString();

      if(!timeOrderedLines.Any())
        return new ExportStateStream(stream, "csv");

      var interval = request.Interval?.ToTimeSpan() ?? default;
      if(interval.TotalMilliseconds < 1)
      {
        foreach(var line in timeOrderedLines)
        {
          WriteRecords(csv, exportData, line.Timestamp, localTimeOffset);
          await csv.NextRecordAsync();
        }
      }
      else
      {
        var startTime = request.Start?.ToDateTime() ?? timeOrderedLines.First().Timestamp;
        var endTime = request.End?.ToDateTime() ?? timeOrderedLines.Last().Timestamp;
        for(var i = startTime; i <= endTime; i += interval)
        {
          WriteRecords(csv, exportData, i, localTimeOffset);
          await csv.NextRecordAsync();
        }
      }
      await csv.FlushAsync();

    }
    stream.Seek(0, SeekOrigin.Begin);

    return new ExportStateStream(stream, "csv");
  }

  private void WriteRecords(CsvWriter csvWriter, IEnumerable<SingleDeviceStateExportData> exportDataCollection, DateTime timestamp, string localTimeOffset)
  {
    csvWriter.WriteField(timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
    csvWriter.WriteField(localTimeOffset);
    foreach(var singleDeviceData in exportDataCollection)
    {
      var exportLine = singleDeviceData.ExportLines
        .Where(innerLine => innerLine.Timestamp <= timestamp)
        .LastOrDefault();
      if(exportLine is null)
      {
        // there should be at least one, otherwise the WriteHeader method would not have written the header
        // if there actually isn't one, then that would be pretty exceptional
        var dummyLine = singleDeviceData.ExportLines.First();
        foreach(var item in dummyLine.ExportItems)
        {
          // nulls are written so that the header alignment is not messed up by simply ignoring the line
          csvWriter.WriteField(null);
        }
      }
      else
      {
        foreach(var item in exportLine.ExportItems)
        {
          csvWriter.WriteField(item.Value);
        }
      }
    }
  }
  private async Task<IOrderedEnumerable<SingleDeviceStateExportData>> GetStateExportData(DeviceStateRequestFilter request)
  {
    var dataProviderGetters = _dataProviders.Select(provider => provider.GetExportData(request));
    var stateExportDataCollectionPerDevice = await Task.WhenAll(dataProviderGetters);
    var combinedStateExportData = stateExportDataCollectionPerDevice.SelectMany(s => s).OrderBy(d => d.DeviceName);

    return combinedStateExportData;
  }

  private static void WriteHeader(CsvWriter writer, IOrderedEnumerable<SingleDeviceStateExportData> data)
  {
    writer.WriteField("Timestamp");
    writer.WriteField("Local Time Offset");
    foreach(var exportData in data)
    {
      var exportLine = exportData.ExportLines.FirstOrDefault();
      if(exportLine is null)
        continue;

      foreach(var exportItem in exportLine.ExportItems)
      {
        writer.WriteField($"{exportLine.DeviceName} - {exportItem.Name}");
      }
    }

    writer.NextRecord();
  }
}
