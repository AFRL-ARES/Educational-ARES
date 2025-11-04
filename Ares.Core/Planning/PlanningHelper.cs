using Ares.Core.Notifications;
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
  private readonly INotifier _notifier;

  public PlanningHelper(IPlannerServiceRepo plannerManager, ILogger<PlanningHelper> logger, INotifier notifier)
  {
    _plannerManager = plannerManager;
    _logger = logger;
    _notifier = notifier;
  }

  public async Task<bool> TryResolveParameters(IEnumerable<PlannerAllocation> plannerAllocations,
    string campaignId,
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
        var planResponse = await planner.Plan(plannableParameters, campaignId, seedExperiments, seedAnalysesArr, cancellationToken);

        if(planResponse.Outcome == Outcome.Failure)
        {
          if(string.IsNullOrWhiteSpace(planResponse.ErrorString))
            await _notifier.Notify("Planner Error!", "Planner reported that planning failed, but did not provide any specific error as to why.", NotificationSeverityEnum.Error);

          else
            await _notifier.Notify($"Planner Reported Error: {planResponse.ErrorString}", "Planner Error!", NotificationSeverityEnum.Error);

          return false;
        }

        if(planResponse.Outcome == Outcome.Warning)
        {
          if(string.IsNullOrWhiteSpace(planResponse.ErrorString))
            await _notifier.Notify("Planner Warning", "Planner reported a warning, but did not provide specific context for that warning.", NotificationSeverityEnum.Warning);

          else
            await _notifier.Notify("Planner Warning", $"Planner successfully planned, but reported a warning: {planResponse.ErrorString}", NotificationSeverityEnum.Warning);
        }
        
        if(planResponse.Outcome == Outcome.Canceled)
          await _notifier.Notify("Planning was canceled.", "Planning was canceled.", NotificationSeverityEnum.Info);

        if(!planResponse.Results.Any())
        {
          await _notifier.Notify("Planning Error!", "Tried to plan for experiment, but planning returned no plan results! Campaign will stop.", NotificationSeverityEnum.Error);
          return false;
        }

        foreach(var result in planResponse.Results)
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
