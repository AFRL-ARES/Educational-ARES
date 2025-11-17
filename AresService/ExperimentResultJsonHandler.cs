using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ares.Core;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Execution;
using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace AresService;

public class ExperimentResultJsonHandler : IExecutionSummaryHandler
{
  private readonly IDeviceStateExportStreamProvider _exportStreamProvider;

  public ExperimentResultJsonHandler(IEnumerable<IDeviceStateExportStreamProvider> exportStreamProviders)
  {
    _exportStreamProvider = exportStreamProviders.OfType<CombinedDeviceStateExportStreamProvider>().First();
    CampaignResultsDirectory = AresConfig.ResultsPath;
  }

  public async Task Handle(ExperimentExecutionSummary result)
  {
    await ExportResults(result, result.ResultOutputPath);
    await ExportDeviceStates(result, result.ResultOutputPath);
  }

  private static Task ExportResults(ExperimentExecutionSummary result, string destinationDirPath)
  {
    var experimentFileName = $"{result.ExperimentId}.json";
    var file = Path.Combine(destinationDirPath, experimentFileName);
    var serializedExp = JsonSerializer.Serialize(result);
    return File.WriteAllTextAsync(file, serializedExp);
  }

  private async Task ExportDeviceStates(ExperimentExecutionSummary result, string destinationDirPath)
  {
    var filter = new DeviceStateRequestFilter
    {
      Start = result.ExecutionInfo.TimeStarted,
      End = result.ExecutionInfo.TimeFinished,
    };

    var stream = await _exportStreamProvider.Export(filter);
    var fileName = $"{result.ExperimentId}_DeviceStates";
    fileName = Path.ChangeExtension(fileName, stream.FileExtension);
    fileName = Path.Combine(destinationDirPath, fileName);
    using var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);
    await stream.Stream.CopyToAsync(fileStream);
    await stream.Stream.DisposeAsync();
  }

  public string CampaignResultsDirectory { get; private set; }
}
