using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote.State;
public class RemoteDeviceStateLoggerFactory(
  ILoggerFactory loggerFactory,
  IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  : DeviceStateLoggerFactory<RemoteDevice>
{
  protected override IDeviceStateLogger Create(RemoteDevice device)
  {
    return new RemoteDeviceStateLogger(dbContextFactory, device, loggerFactory.CreateLogger<RemoteDeviceStateLogger>());
  }
}
