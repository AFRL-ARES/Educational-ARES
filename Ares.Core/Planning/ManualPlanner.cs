using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Grpc.Core;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Planning;

public class ManualPlanner : IPlannerService
{
  private readonly Queue<IEnumerable<ManualPlanResult>> _planResultsQueue = new();
  private readonly ISubject<State> _plannerStateSubject = new BehaviorSubject<State>(State.Active);

  public ManualPlanner()
  {
    Status = new ConnectionStatus { Status = AresStatus.Connected, Info = "Manual Planner is active!" };
    PlannerServiceStateObservable = _plannerStateSubject.AsObservable();
  }

  public IEnumerable<IEnumerable<(string Name, AresValue Value)>> CurrentPlanResults => 
    _planResultsQueue
    .AsEnumerable()
    .Select(results => results
    .Select(result => (result.Name, result.Value)));


  public Task<PlanResponse> Plan(IEnumerable<ParameterMetadata> plannableParameters,
    RequestMetadata metadata,
    IEnumerable<ExperimentOverview> experiments, 
    IEnumerable<Analysis> _, 
    CancellationToken __)
  {
    try
    {
      var currentParameterSet = _planResultsQueue.Dequeue().ToList();
      var returnList = plannableParameters.Select(metadata => currentParameterSet.First(result => result.Name == metadata.Name).ToPlanResult(metadata)).ToList();
      var response = new PlanResponse(returnList, Outcome.Success, string.Empty);
      return Task.FromResult(response);
    }
    catch(InvalidOperationException e)
    {
      return Task.FromResult(new PlanResponse([], Outcome.Failure, e.Message));
    }
  }

  public Task Seed(ManualPlannerSeed seedParam)
  {
    Reset();
    switch(seedParam.PlannerStuffCase)
    {
      case ManualPlannerSeed.PlannerStuffOneofCase.None:
        break;
      case ManualPlannerSeed.PlannerStuffOneofCase.PlannerValues:
        var manualPlanResultCollections = seedParam.PlannerValues.PlannedValues
          .Select(set => set.ParameterValues
          .Select(pair => new ManualPlanResult(pair.Name, pair.Value)));

        foreach(var manualPlanResults in manualPlanResultCollections)
          _planResultsQueue.Enqueue(manualPlanResults);

        break;
      case ManualPlannerSeed.PlannerStuffOneofCase.FileLines:
        LoadPlanQueue(seedParam.FileLines.PlannerValues);
        break;
      default:
        throw new ArgumentOutOfRangeException($"Parameter was out of range for Manual Planner Seed! {seedParam.PlannerStuffCase}");
    }

    return Task.CompletedTask;
  }

  public void Reset()
  {
    _planResultsQueue.Clear();
  }

  public Task Init()
  {
    var manualPlanner = new Planner()
    {
      PlannerName = "Manual Planner",
      Description = "A planner used for executing sets of manual values.",
      UniqueId = UniqueId,
      Version = Version.ToString()
    };

    AvailablePlanners.Add(manualPlanner);
    return Task.CompletedTask;
  }

  private void LoadPlanQueue(IEnumerable<string> lines, char delim = ',')
  {
    // Read all lines from the data file
    List<string> dataFileLines = lines.ToList();

    // Make sure the data file has data!
    if(dataFileLines == null || dataFileLines.Count < 2)
      throw new Exception("Could not read any data from file!");

    // Create a useful Func to split lines
    var tokenizeLine = new Func<string, List<string>>(line =>
    {
      line = line.Trim();
      return line.Split(delim, StringSplitOptions.RemoveEmptyEntries).ToList();
    });

    // Tokenize the first line of the file
    List<string> firstLineTokens = tokenizeLine(dataFileLines.First());

    // Ensure the validity of the data descriptions then remove them from the list
    firstLineTokens.ForEach(desc =>
    {
      if(string.IsNullOrEmpty(desc.Trim()))
        throw new Exception("Data descriptions in file cannot be null or empty!");
    });

    dataFileLines.RemoveAt(0);

    // Tokenize each line and parse to doubles
    int expNum = 1;// 1 based index
    List<List<string>> data = [];
    dataFileLines.ForEach(expDataLine =>
    {
      // Tokenize the line and check the validity of it
      expNum += 1;
      List<string> expLineTokens = tokenizeLine(expDataLine);
      if(expLineTokens.Count != firstLineTokens.Count)
        throw new Exception("Experiment " + expNum + " does not have enough tokens in line!");

      // Parse the tokens to double and check the validities
      int tokenNum = 0;// 1 based index
      List<string> expData = [];
      expLineTokens.ForEach(dataToken =>
      {
        tokenNum += 1;
        if(string.IsNullOrEmpty(dataToken.Trim()))
          throw new Exception("Experiment " + expNum + ", column " + tokenNum + " data is null or empty!");

        expData.Add(dataToken);
      });

      // This line is a good experiment
      data.Add(expData);
    });
    
    foreach(var argList in data)
    {
      var planResults = argList.Select((d, i) => new ManualPlanResult(firstLineTokens[i], AresValueHelper.CreateString(d)));
      _planResultsQueue.Enqueue(planResults);
    }
  }

  public void UpdateSettings(AresStruct settings)
  {
    throw new NotImplementedException();
  }

  public Task<PlannerServiceCapabilities> GetCapabilities(CancellationToken cancellationToken = default)
  {
    var response = new PlannerServiceCapabilities() { ServiceName = "Manual Planner", SettingsSchema = new AresDataSchema(), TimeoutSeconds = long.MaxValue };
    response.AcceptedTypes.Add(AresDataType.Number);
    response.AcceptedTypes.Add(AresDataType.String);
    response.AvailablePlanners.AddRange(AvailablePlanners);
    return Task.FromResult(response);
  }

  public async Task<PlanResponse> Plan(IEnumerable<ParameterMetadata> plannableParameters, RequestMetadata metadata, IEnumerable<ExperimentOverview> previousExperiments, IEnumerable<Analysis> analysisHistory, AresStruct settings, CancellationToken cancellationToken = default)
  {
    return await Plan(plannableParameters, metadata, previousExperiments, analysisHistory, cancellationToken);
  }

  private record ManualPlanResult(string Name, AresValue Value)
  {
    public PlanResult ToPlanResult(ParameterMetadata metadata)
      => new(metadata, Value);
  }

  public string UniqueId { get; set; } = new Guid().ToString();
  public ConnectionStatus Status { get; protected set; }
  public string Name { get; set; } = "Manual Planner";
  public string Address { get; set; } = string.Empty;
  public IList<Planner> AvailablePlanners { get; } = [];
  public string Type { get; } = "Manual Planner";
  public string Version { get; } = "1.0.0";
  public string Description { get; } = "A planner designed for executing a pre-determined set of values";
  public IObservable<State> PlannerServiceStateObservable { get; }
  public State PlannerServiceState { get; } = State.Active;
  public string StateMessage { get; } = "Manual Planner is active!";
  public AresStruct Settings { get; } = new AresStruct();
  public TimeSpan PlanningTimeout { get; } = TimeSpan.MaxValue;
}
