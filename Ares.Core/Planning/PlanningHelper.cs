using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Planning;

public class PlanningHelper : IPlanningHelper
{
  private readonly IPlannerServiceRepo _plannerManager;
  private readonly ILogger<PlanningHelper> _logger;

  public PlanningHelper(IPlannerServiceRepo plannerManager, ILogger<PlanningHelper> logger)
  {
    _plannerManager = plannerManager;
    _logger = logger;
  }

  public async Task<bool> TryResolveParameters(IEnumerable<PlannerAllocation> plannerAllocations,
    IEnumerable<Parameter> parameters,
    IEnumerable<Analysis> seedAnalyses,
    IEnumerable<ExperimentOverview> seedExperiments,
    CancellationToken cancellationToken)
  {
    var parameterArray = parameters.ToArray();
    var plannerToMetadataMaps = new List<(IPlannerService Planner, ParameterMetadata Metadata)>();
    foreach(var plannerAllocation in plannerAllocations)
    {
      var planner = _plannerManager.GetPlannerById(plannerAllocation.Planner.UniqueId);
      if(planner is null)
        return false;

      plannerToMetadataMaps.Add((planner, plannerAllocation.Parameter));
    }

    var planGroup = plannerToMetadataMaps.GroupBy(pair => pair.Planner);
    var seedAnalysesArr = seedAnalyses.ToArray();
    foreach(var grouping in planGroup)
    {
      var planner = grouping.Key;
      try
      {
        var plannableParameters = grouping.Select(pair => pair.Metadata).ToArray();
        var resultsEnumerable = await planner.Plan(plannableParameters, seedExperiments, seedAnalysesArr, cancellationToken);
        var results = resultsEnumerable.ToArray();
        if(!results.Any())
          return false;

        foreach(var result in results)
        {
          var parameterPlanTarget = parameterArray.FirstOrDefault(parameter => parameter.PlanningMetadata.UniqueId == result.Metadata.UniqueId);

          if(parameterPlanTarget is null)
            continue;

          parameterPlanTarget.Value = result.Value;
        }
      }
      catch(Exception e)
      {
        _logger.LogError("Failed to plan. {}", e);
        return false;
      }
    }

    return true;
  }
}
