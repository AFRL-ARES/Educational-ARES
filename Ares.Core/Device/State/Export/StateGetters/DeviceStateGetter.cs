using Ares.Core.Device.Helpers;
using Ares.Core.Device.State.Logging;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device.State.Export.StateGetters;

public class DeviceStateGetter : IDeviceStateGetter
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly DeviceIdHelper _deviceIdHelper;

  public DeviceStateGetter(IDbContextFactory<CoreDatabaseContext> dbContextFactory, DeviceIdHelper deviceIdHelper)
  {
    _dbContextFactory = dbContextFactory;
    _deviceIdHelper = deviceIdHelper;
  }

  public async Task<IDictionary<string, IEnumerable<TState>>> GetStates<TState>(DeviceStateRequestFilter request) where TState : class, IDeviceState
  {
    using var context = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<TState>(request, context);
    var stateMap = stateQuery
      .GroupBy(s => s.DeviceId)
      .ToDictionary(g => _deviceIdHelper.DeviceIdToName(g.Key), g => g.AsEnumerable());
    
    return stateMap;
  }
}