using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device.State.Logging;
public static class DeviceStateQueryBuilder
{
  public static async Task<IQueryable<T>> BuildQuery<T>(DeviceStateRequestFilter request, CoreDatabaseContext dbContext) where T : class, IDeviceState
  {
    var statesQuery = dbContext.Set<T>().AsQueryable();
    if(request.Start is not null)
    {
      statesQuery = statesQuery.Where(state => state.Timestamp >= request.Start);
    }
    if(request.End is not null)
    {
      statesQuery = statesQuery.Where(state => state.Timestamp <= request.End);
    }
    if(request.DeviceIds.Any())
    {
      statesQuery = statesQuery.Where(state => request.DeviceIds.Contains(state.DeviceId));
    }
    if(!string.IsNullOrEmpty(request.CompletedCampaignId))
    {
      var completedCampaign = await dbContext.CampaignExecutionSummaries.FirstOrDefaultAsync(result => result.CampaignId == request.CompletedCampaignId);
      if(completedCampaign is not null)
        statesQuery = statesQuery.Where(state => state.Timestamp >= completedCampaign.ExecutionInfo.TimeStarted && state.Timestamp <= completedCampaign.ExecutionInfo.TimeFinished);
    }
    if(!string.IsNullOrEmpty(request.CompletedExperimentId))
    {
      var completedExperiment = await dbContext.CampaignExecutionSummaries
        .SelectMany(result => result.ExperimentSummaries)
        .FirstOrDefaultAsync(result => result.ExperimentOverview.UniqueId == request.CompletedExperimentId);

      if(completedExperiment is not null)
        statesQuery = statesQuery.Where(state => state.Timestamp >= completedExperiment.ExecutionInfo.TimeStarted && state.Timestamp <= completedExperiment.ExecutionInfo.TimeFinished);
    }

    return statesQuery;
  }
}
