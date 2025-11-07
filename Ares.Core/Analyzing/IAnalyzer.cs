using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Connection;

namespace Ares.Core.Analyzing;

public interface IAnalyzer
{
  /// <summary>
  /// Ares user given name for the analyzer (can be useful when multiple analyzers of same type have to be used)
  /// </summary>
  string Name { get; set; }

  /// <summary>
  /// The analyzer itself should have a type name that it reports regardless of what name the user has given it.
  /// So for example the type could be "BoraasPlanner" while the user calls it "My Amazing Planner"
  /// </summary>
  string Type { get; }

  /// <summary>
  /// Version of the analyzer, provided directly by the analyzer and not set by user.
  /// </summary>
  string Version { get; }

  /// <summary>
  /// The unique id for identifying the analyzer.
  /// </summary>
  string UniqueId { get; internal set; }

  /// <summary>
  /// Optional description of the analyzer.
  /// </summary>
  string Description { get; }

  /// <summary>
  /// Provides a way for any initialization logic to be run upon the creation of the analyzer if there's a need
  /// </summary>
  /// <returns></returns>
  Task Init();

  /// <summary>
  /// Provides an observable for the <see cref="State" />
  /// </summary>
  IObservable<State> AnalyzerStateObservable { get; }

  /// <summary>
  /// Current state (<see cref="State" />) of the analyzer which essentially indicated
  /// whether or not this analyzer is currently available for analyzing
  /// </summary>
  State AnalyzerState { get; }

  /// <summary>
  /// If any reasoning needs to be provided for the current <see cref="State"/>
  /// </summary>
  string StateMessage { get; }

  /// <summary>
  /// Optional inputs that live on analyzers that can guide the analysis in certain directions.
  /// Unlike parameters which are generally supposed to be different per analysis, the settings
  /// give the ability to have constants throughout the different analyses
  /// </summary>
  AresStruct Settings { get; }

  /// <summary>
  /// Updates the internal settings by overwriting the existing values with the ones provided by the
  /// passed in settings argument
  /// </summary>
  void UpdateSettings(AresStruct settings);

  /// <summary>
  /// We give an option for analyzer to receive descriptions of inputs that ARES plans on sending it, and then
  /// the analyzer can let ARES know ahead of time if it's capable of dealing with the given inputs.
  /// That way we don't start an experiment and fail because ARES sent an input that was incompatible
  /// </summary>
  /// <param name="inputSchema">The inputs we plan to send to the analyzer</param>
  /// <returns>Result of the validation along with a message if there is an error</returns>
  Task<ParameterValidationResult> ValidateInputs(AresDataSchema inputSchema, CancellationToken cancellationToken = default);

  /// <summary>
  /// Returns supported parameters that ARES should provide. Some are optional, some required.
  /// </summary>
  /// <returns></returns>
  Task<AresDataSchema> GetParameters(CancellationToken cancellationToken = default);

  /// <summary>
  /// This will return some custom settings that the analyzer supports in addition to the parameters.
  /// One of the built-in settings is the timeout that can tell ARES to wait a little longer in case analysis is going
  /// to take a while.
  /// Other settings are analyzer-specific and provided to ARES as descriptions. The user can then fill out the settings
  /// and send them back during analysis.
  /// </summary>
  /// <returns></returns>
  Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken = default);

  /// <summary>
  /// Does the actual analysis work.
  /// </summary>
  /// <param name="inputs">The experiment outputs to analyze in the form of the <see cref="AnalysisInput" /> proto message</param>
  /// <param name="cancellationToken"></param>
  /// <param name="settings">Optional list of settings to influence the analysis</param>
  /// <returns><see cref="Analysis" /> which is the outcome of the analysis performed.</returns>
  Task<Analysis> Analyze(AresStruct inputs, AresStruct settings, RequestMetadata metadata, CancellationToken cancellationToken = default);

  /// <summary>
  /// Does the actual analysis work.
  /// </summary>
  /// <param name="inputs">The experiment outputs to analyze in the form of the <see cref="AnalysisInput" /> proto message</param>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Analysis" /> which is the outcome of the analysis performed.</returns>
  Task<Analysis> Analyze(AresStruct inputs, RequestMetadata metadata, CancellationToken cancellationToken);

  /// <summary>
  /// How long do we expect the analyzer to do its analysis before ARES decides that analyzing has failed.
  /// This is here in case the analysis takes a few minutes
  /// </summary>
  TimeSpan AnalysisTimeout { get; }
}
