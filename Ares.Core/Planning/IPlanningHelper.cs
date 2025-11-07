using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;

namespace Ares.Core.Planning;

/// <summary>
/// Optional helper made to alleviate the need to manually find and use planners
/// </summary>
public interface IPlanningHelper
{
  /// <summary>
  /// Will try and resolve the values for the given parameters based on the given planner allocations.
  /// The values will be added directly to the given parameters.
  /// </summary>
  /// <param name="plannerAllocations">A collection of planner-to-parameter allocations indicating which planner to pick</param>
  /// <param name="parameters">Collection of parameters whose values to plan for</param>
  /// <param name="seedAnalyses">The completed experiment analyses used to seed the plan</param>
  /// <returns>True if planning succeeded, false otherwise</returns>
  Task<bool> TryResolveParameters(IEnumerable<PlannerAllocation> plannerAllocations,
    RequestMetadata metadata,
    IEnumerable<Parameter> parameters,
    IEnumerable<Analysis> seedAnalyses,
    IEnumerable<ExperimentOverview> seedExperiments,
    CancellationToken cancellationToken);
}
