using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;

namespace Ares.Core.Tests.Data.Analyzer;

public class TestReplyAnalyzer : AnalyzerBase
{
  public TestReplyAnalyzer() : base("Test Analyzer", "TestAnalyzer", "1.0")
  {
  }

  public override Task<Analysis> Analyze(AresStruct inputs, RequestMetadata metadata, CancellationToken cancellationToken)
  {
    var firstData = inputs.Fields["TestAnalyzerInput"];
    var analysis = new Analysis() { Result = (float)firstData.NumberValue, AnalysisOutcome = Outcome.Success };

    return Task.FromResult(analysis);
  }

  public override Task<Analysis> Analyze(AresStruct inputs, AresStruct settings, RequestMetadata metadata, CancellationToken cancellationToken)
  {
    return Analyze(inputs, metadata, cancellationToken);
  }

  public override Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken)
  {
    return Task.FromResult(new AnalyzerCapabilities());
  }

  public override Task<AresDataSchema> GetParameters(CancellationToken cancellationToken)
  {
    var schema = new AresDataSchema();
    var testReplySchema = new SchemaEntry() { Optional = false, Type = AresDataType.Number };

    schema.Fields["TestReply"] = testReplySchema;

    return Task.FromResult(schema);
  }
}
