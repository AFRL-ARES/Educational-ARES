using Ares.Core.Planning;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;

namespace Ares.Core.Tests;

internal class ManualPlannerTests
{

  private readonly IEnumerable<string> _fileLines = new[]
  {
    "TestParameter1,TestParameter2,TestParameter3",
    "111,222,333",
    "111.1,222.2,333.3"
  };
  private readonly ManualPlanner _manualPlanner = new();

  private readonly IEnumerable<ParameterMetadata> _parameterMetadatas = new[]
  {
    new ParameterMetadata
    {
      Index = 0,
      Name = "TestParameter1",
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "C"
    },
    new ParameterMetadata
    {
      Index = 1,
      Name = "TestParameter2",
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "C"
    },
    new ParameterMetadata
    {
      Index = 2,
      Name = "TestParameter3",
      UniqueId = Guid.NewGuid().ToString(),
      Unit = "C"
    }
  };

  [SetUp]
  public void Setup()
  {
    _manualPlanner.Reset();
  }

  [Test]
  public async Task CorrectNumberOfResults_FileLines()
  {
    var plannerSeed = new ManualPlannerSeed();
    plannerSeed.FileLines = new ManualPlannerFileLines();
    plannerSeed.FileLines.PlannerValues.AddRange(_fileLines);
    await _manualPlanner.Seed(plannerSeed);
    var planResponse = await _manualPlanner.Plan(_parameterMetadatas, string.Empty, Array.Empty<ExperimentOverview>(), Array.Empty<Analysis>(), CancellationToken.None);
    Assert.That(planResponse.Results, Has.Exactly(3).Items);
  }
}
