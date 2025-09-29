using Ares.Core.Planning;
using Ares.Datamodel.Connection;

namespace Ares.Core.Execution.StartConditions;

public class AssignedPlannersActiveStartCondition : IStartCondition
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IPlannerServiceRepo _plannerRepo;

  public AssignedPlannersActiveStartCondition(IActiveCampaignTemplateStore activeCampaignTemplateStore, IPlannerServiceRepo plannerRepo)
  {
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _plannerRepo = plannerRepo;
  }

  public Task<StartConditionResult> CanStart()
  {
    if(_activeCampaignTemplateStore.CampaignTemplate?.ExperimentTemplate is null)
      return Task.FromResult(new StartConditionResult(false, "The active campaign template store had a null experiment template."));

    var assignedPlanners = _activeCampaignTemplateStore.CampaignTemplate.PlannerAllocations.Select(allocation => allocation.Planner);

    foreach(var plannerService in assignedPlanners)
    {
      var planner = _plannerRepo.GetPlannerByName(plannerService.Name);

      if(planner is null)
        return Task.FromResult(new StartConditionResult(false, $"Assigned Planner Adapter {plannerService.Name} could not be found in the core."));

      if(planner.PlannerServiceState != State.Active)
        return Task.FromResult(new StartConditionResult(false, $"Assigned Planner Adapter {plannerService.Name} is not connected to ARES."));
    }

    return Task.FromResult(new StartConditionResult(true));
  }

}
