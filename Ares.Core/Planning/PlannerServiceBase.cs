using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Planning;

/// <summary>
/// A base implementation of the planner interface.
/// </summary>

public abstract class PlannerServiceBase : IPlannerService
{
  State _plannerState = State.UnspecifiedState;
  private readonly BehaviorSubject<State> _plannerStateSubject = new(State.UnspecifiedState);

  public PlannerServiceBase(string name, string type, string version)
  {
    Name = name;
    Type = type;
    Version = version;
    PlannerServiceStateObservable = _plannerStateSubject.AsObservable();
  }

  public PlannerServiceBase(string name, string type, string version, string uniqueId)
  {
    Name = name;
    Type = type;
    Version = version;
    PlannerServiceStateObservable = _plannerStateSubject.AsObservable();
    UniqueId = uniqueId;
  }

  public State PlannerServiceState
  {
    get => _plannerState;

    protected set
    {
      _plannerState = value;
      _plannerStateSubject.OnNext(State.UnspecifiedState);
    }
  }

  public void UpdateSettings(AresStruct settings)
  {
    foreach(var setting in Settings.Fields)
    {
      var newValue = settings.Fields.GetValueOrDefault(setting.Key);
      if(newValue is null)
      {
        continue;
      }

      Settings.Fields[setting.Key] = newValue;
    }
  }

  public abstract Task<PlanResponse> Plan(IEnumerable<ParameterMetadata> plannableParameters,
    string campaignId,
    IEnumerable<ExperimentOverview> previousExperiments,
    IEnumerable<Analysis> analysisHistory, 
    CancellationToken cancellationToken = default);

  public abstract Task<PlanResponse> Plan(IEnumerable<ParameterMetadata> plannableParameters,
    string campaignId,
    IEnumerable<ExperimentOverview> previousExperiments,
    IEnumerable<Analysis> analysisHistory,
    AresStruct settings,
    CancellationToken cancellationToken = default);

  public abstract Task<PlannerServiceCapabilities> GetCapabilities(CancellationToken cancellationToken);

  public virtual Task Init()
  {
    return Task.CompletedTask;
  }

  public string Name { get; set; }
  public string Version { get; protected set; }
  public string Type { get; protected set; }
  public AresStruct Settings { get; } = new();
  public string UniqueId { get; set; } = Guid.NewGuid().ToString();
  public IObservable<State> PlannerServiceStateObservable { get; }
  public string Description { get; protected set; } = "";
  public string StateMessage { get; protected set; } = "";
  public TimeSpan PlanningTimeout { get; protected set; } = TimeSpan.FromSeconds(30);
  public AresStruct PlannerServiceSettings { get; } = new();
  public IList<Planner> AvailablePlanners { get; } = [];
}
