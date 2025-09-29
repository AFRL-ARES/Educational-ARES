using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;

namespace Ares.Core.Analyzing;

/// <summary>
/// Analyzer that returns a 0 as its analysis result.
/// Used as a default analyzer in case no actual analyzers are present
/// </summary>
internal class NoneAnalyzer : AnalyzerBase
{
  public static readonly string Id = "NONE-ANALYZER";

  public NoneAnalyzer() : base("NONE", "NONE :)", "1.0.0")
  {
    UniqueId = Id;
    AnalyzerState = State.Active;
  }

  public override Task<Analysis> Analyze(AresStruct inputs, CancellationToken cancellationToken)
  {
    var analysis = new Analysis
    {
      Success = true,
      Result = 0
    };

    return Task.FromResult(analysis);
  }

  public override Task<Analysis> Analyze(AresStruct inputs, AresStruct _settings, CancellationToken cancellationToken)
  {
    return Analyze(inputs, cancellationToken);
  }

  public override Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken)
  {
    var capability = new AnalyzerCapabilities { TimeoutSeconds = long.MaxValue };

    return Task.FromResult(capability);
  }

  public override Task<AresDataSchema> GetParameters(CancellationToken cancellationToken)
  {
    return Task.FromResult(new AresDataSchema());
  }
}
