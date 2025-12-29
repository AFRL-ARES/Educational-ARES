using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Templates;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Analyzing;

public class AnalysisHelper
{
  readonly IAnalyzerRepo _analyzerRepo;
    private readonly ILogger<AnalysisHelper> _logger;

    public AnalysisHelper(IAnalyzerRepo analyzerRepo, ILogger<AnalysisHelper> logger)
    {
      _analyzerRepo = analyzerRepo;
      _logger = logger;
    }

  public async Task<Analysis> Analyze(ExperimentTemplate template, ExperimentExecutionSummary experimentSummary, RequestMetadata metadata, CancellationToken cancellationToken)
  {
    try
    {
      var analyzer = GetAnalyzer(template.AnalyzerId);
      _logger.LogInformation("Analysis requested using analyzer {AnalyzerName}", analyzer.Name);
      var analyzerInputs = ExperimentOutputToAnalyzerInputs(
        experimentSummary.ExperimentOverview.Result,
        template.AnalyzerMaps);
      // TODO: Maybe add support for settings overrides if needed
      var analysis = await analyzer.Analyze(analyzerInputs, metadata, cancellationToken);
      experimentSummary.ExperimentOverview.AnalysisOverview = new AnalysisOverview
      {
        UniqueId = Guid.NewGuid().ToString(),
        Result = analysis.Result,
        AnalyzerInfo = await analyzer.CreateAnalyzerInfo(),
        ExperimentOverviewId = experimentSummary.ExperimentOverview.UniqueId
      };

      _logger.LogInformation("Analysis completed {Result}", analysis.Result);
      return analysis;
    }
    catch(RpcException e)
    {
      if(e.InnerException is OperationCanceledException oce)
      {
        return new Analysis { Result = float.NaN, AnalysisOutcome = Outcome.Canceled, ErrorString = $" Analysis has been canceled" };
      }
      return new Analysis {Result = float.NaN, AnalysisOutcome = Outcome.Failure, ErrorString = $"Call to analyzer has failed: {e}" };
    }
    catch(Exception e)
    {
      return new Analysis { Result = float.NaN, AnalysisOutcome = Outcome.Failure, ErrorString = $"Call to analyzer has failed: {e}" };
    }
  }

  private IAnalyzer GetAnalyzer(string? analyzerId)
  {
    if(analyzerId is null)
    {
      var noneAnalyzer = _analyzerRepo.GetAnalyzerById(NoneAnalyzer.Id);
      if(noneAnalyzer is null)
      {
        throw new InvalidOperationException(
          "No analyzer provided and the default NONE analyzer was not found.");
      }

      return noneAnalyzer;
    }

    return _analyzerRepo
    .GetAnalyzerById(analyzerId) ?? throw new InvalidOperationException($"Could not find desired analyzer with id {analyzerId}");
  }

  private AresStruct ExperimentOutputToAnalyzerInputs(AresStruct experimentResult, MapField<string, string> analyzerMappings)
  {
    var mappedStruct = new AresStruct();
    // Analyzer mapping is [KeyThatAnalyzerExpects, UserDefinedExperimentOutputKey]
    foreach(var map in analyzerMappings)
    {
      var expResultValue = experimentResult.Fields[map.Value];
      mappedStruct.Fields[map.Key] = expResultValue;
    }

    return mappedStruct;
  }
}
