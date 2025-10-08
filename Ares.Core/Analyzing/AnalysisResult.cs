using Ares.Datamodel.Analyzing;

namespace Ares.Core.Analyzing;
public record AnalysisResult(Analysis? Analysis, AnalysisResultType ResultType, string? Error = null);

public enum AnalysisResultType
{
  Success,
  Failure,
  Canceled
}