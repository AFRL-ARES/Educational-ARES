using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components.Forms;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Automation.Planning;

public class ManualPlannerViewModel : ReactiveObject
{
  private readonly AresPlannerManagementService.AresPlannerManagementServiceClient _client;

  public ManualPlannerViewModel(AresPlannerManagementService.AresPlannerManagementServiceClient client)
  {
    _client = client;
    _ = UpdatePlannerValues();
  }

  public async Task UpdatePlannerValues()
  {
    var collection = await _client.GetManualPlannerSeedAsync(new Empty());
    ManualPlannerValues = collection.PlannedValues;
  }

  public async Task<bool> FileUploaded(IBrowserFile file)
  {
    NumberOfPlannedExperiments = 0;
    var collection = new ManualPlannerSetCollection();
    var stream = file.OpenReadStream();
    var reader = new StreamReader(stream);
    var result = await reader.ReadLineAsync();
    var header = result?.Split(',', StringSplitOptions.TrimEntries);
    if(header is null)
      return false;

    PlannerValueHeaders = header.ToList();

    result = await reader.ReadLineAsync();
    while(result is not null)
    {
      try
      {
        var splitResult = result.Split(',');
        var plannerSet = new ManualPlannerSet();
        plannerSet.ParameterValues.AddRange(splitResult.Select((s, i) => new ParameterNameValuePair { Name = header[i], Value = ParseToAresValue(s)}));
        collection.PlannedValues.Add(plannerSet);
        NumberOfPlannedExperiments += 1;
      }
      catch(Exception)
      {
        return false;
      }

      result = await reader.ReadLineAsync();
    }

    if(!collection.PlannedValues.Any())
      return true;

    await _client.SeedManualPlannerAsync(new ManualPlannerSeed { PlannerValues = collection });
    await UpdatePlannerValues();

    return true;
  }

  private AresValue ParseToAresValue(string item)
  {
    if(string.IsNullOrEmpty(item))
      return AresValueHelper.CreateNull();

    if(item.StartsWith("\"") && item.EndsWith("\"") && item.Length > 1)
    {
      return AresValueHelper.CreateString(item);
    }

    var parsed = double.TryParse(item, out var value);

    if(!parsed)
      return AresValueHelper.CreateNull();

    return AresValueHelper.CreateNumber(value);
  }

  public Task CreateDisplayData()
  {
    DisplayObjects.Clear();
    var experimentNumber = 1;
    foreach(var item in ManualPlannerValues)
    {
      var displayObject = new ManualPlannerDisplayObject();
      displayObject.ExperimentNumber = $"{experimentNumber}";
      displayObject.Parameters = item;
      DisplayObjects.Add(displayObject);
      experimentNumber++;
    }

    return Task.CompletedTask;
  }

  [Reactive]
  public IEnumerable<ManualPlannerSet> ManualPlannerValues { get; private set; } = Array.Empty<ManualPlannerSet>();

  public List<ManualPlannerDisplayObject> DisplayObjects { get; set; } = [];

  public List<string> PlannerValueHeaders { get; set; } = new List<string>();

  public int NumberOfPlannedExperiments { get; set; } = 0;
}
