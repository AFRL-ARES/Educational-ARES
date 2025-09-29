using System.Collections.ObjectModel;
using Ares.Datamodel.Analyzing;

namespace Ares.Core.Analyzing;

// This is a non-persistent storage of analyses as they come out of analyzers
// mainly used to instanced functionality like tracking the analysis results to
// decide when to stop the campaign
public class AnalysisRepo : Collection<Analysis>
{
  public void StoreAnalysis(Analysis analysis)
  {
    Add(analysis);
  }

  public void ClearAnalyses()
  {
    Clear();
  }
}
