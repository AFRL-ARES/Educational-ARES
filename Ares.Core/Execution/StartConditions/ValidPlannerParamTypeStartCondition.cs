using Ares.Core.Planning;

namespace Ares.Core.Execution.StartConditions;

internal class ValidPlannerParamTypeStartCondition : IStartCondition
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IPlannerServiceRepo _plannerRepo;

  public ValidPlannerParamTypeStartCondition(IActiveCampaignTemplateStore activeCampaignTemplateStore, IPlannerServiceRepo plannerRepo)
  {
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _plannerRepo = plannerRepo;
  }

  public async Task<StartConditionResult> CanStart()
  {
    if(_activeCampaignTemplateStore.CampaignTemplate?.ExperimentTemplate is null)
      return new StartConditionResult(false, "The active campaign template store had a null experiment template.");

    var allocations = _activeCampaignTemplateStore.CampaignTemplate.PlannerAllocations;

    foreach(var allocation in allocations)
    {
      var paramType = allocation.Parameter.Schema.Type;
      var planner = _plannerRepo.GetPlannerById(allocation.Planner.UniqueId);

      if(planner is null)
        return new StartConditionResult(false, $"Failed to find associated planner {allocation.Planner.Name}");

      var capabilities = await planner.GetCapabilities();

      if(!capabilities.AcceptedTypes.Contains(paramType))
      {
        var message = $"{allocation.Planner.Name} was assigned to the parameter " +
          $"{allocation.Parameter.Name}, but {allocation.Planner.Name} " +
          $"does not support inputs of type {paramType}";
        return new StartConditionResult(false, message);
      }
    }

    return new StartConditionResult(true);

  }
}
