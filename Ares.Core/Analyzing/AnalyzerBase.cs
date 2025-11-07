using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Connection;

namespace Ares.Core.Analyzing;

/// <summary>
/// A base implementation of the analyzer interface featuring some basic parameter validation.
/// The validation just checks to make sure all the required inputs are filled and the requested vs given types match
/// </summary>
public abstract class AnalyzerBase : IAnalyzer
{
  State _analyzerState = State.UnspecifiedState;
  private readonly ISubject<State> _analyzerStateSubject = new BehaviorSubject<State>(State.UnspecifiedState);

  public AnalyzerBase(string name, string type, string version)
  {
    Name = name;
    Type = type;
    Version = version;
    AnalyzerStateObservable = _analyzerStateSubject.AsObservable();
  }

  public AnalyzerBase(string name, string type, string version, string uniqueId)
  {
    Name = name;
    Type = type;
    Version = version;
    AnalyzerStateObservable = _analyzerStateSubject.AsObservable();
    UniqueId = uniqueId;
  }

  public string Name { get; set; }
  public string Version { get; protected set; }
  public string Type { get; protected set; }
  public AresStruct Settings { get; } = new();
  public string UniqueId { get; set; } = Guid.NewGuid().ToString();
  public IObservable<State> AnalyzerStateObservable { get; }

  public State AnalyzerState
  {
    get => _analyzerState;
    protected set
    {
      _analyzerState = value;
      _analyzerStateSubject.OnNext(value);
    }
  }
  public string Description { get; protected set; } = "";

  public string StateMessage { get; protected set; } = "";

  public TimeSpan AnalysisTimeout { get; protected set; } = TimeSpan.FromSeconds(5);

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

  public abstract Task<Analysis> Analyze(AresStruct inputs, RequestMetadata metadata, CancellationToken cancellationToken);

  public abstract Task<Analysis> Analyze(AresStruct inputs, AresStruct settings, RequestMetadata metadata, CancellationToken cancellationToken);

  public abstract Task<AresDataSchema> GetParameters(CancellationToken cancellationToken);

  private static ParameterValidationResult ValidateParameterTypes(KeyValuePair<string, SchemaEntry> analyzerField, AresDataSchema parameters)
  {
    var result = new ParameterValidationResult();
    var matchingAnalysisParameter = parameters.Fields.GetValueOrDefault(analyzerField.Key);
    if(matchingAnalysisParameter is null)
    {
      result.Success = false;
      result.Messages.Add($"Analyzer does not support with key of {analyzerField.Key}.");
      return result;
    }

    if(matchingAnalysisParameter.Type != analyzerField.Value.Type)
    {
      result.Success = false;
      result.Messages
        .Add(
          $"Parameter type mismatch. The provided input {analyzerField.Key} has a type of " +
          $"{analyzerField.Value.Type.ToString()} which doesn't match the type expected by the analyzer which is " +
          $"{matchingAnalysisParameter.Type.ToString()}");
    }

    result.Success = true;
    return result;
  }

  private static ParameterValidationResult ValidateRequiredParams(AresDataSchema inputSchema, AresDataSchema parameters)
  {
    var requiredParams = parameters.Fields.Where(p => !p.Value.Optional).ToArray();
    var unfulfilledParams = requiredParams.Where(rp => !inputSchema.Fields.Any(input => input.Key == rp.Key && input.Value.Type == rp.Value.Type));

    var messages = unfulfilledParams.Select(up => $"No value provided for the required parameter {up.Key}.").ToArray();
    var result = new ParameterValidationResult
    {
      Success = !messages.Any()
    };
    result.Messages.AddRange(messages);

    return result;
  }

  public virtual async Task<ParameterValidationResult> ValidateInputs(AresDataSchema inputSchema, CancellationToken cancellationToken)
  {
    var analysisSchema = await GetParameters(cancellationToken);
    var paramValidationResults = analysisSchema.Fields.Select(analyzerField => ValidateParameterTypes(analyzerField, analysisSchema)).ToArray();
    var requiredValidationResult = ValidateRequiredParams(inputSchema, analysisSchema);
    var allValidationResults = paramValidationResults.Append(requiredValidationResult).ToArray();

    var result = new ParameterValidationResult { Success = allValidationResults.All(r => r.Success) };
    result.Messages.AddRange(allValidationResults.SelectMany(r => r.Messages));

    return result;
  }

  public abstract Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken);

  public virtual Task Init()
  {
    return Task.CompletedTask;
  }
}
