using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using MK4S.Config;
using PrusaMK4S;

namespace AresService.ConfigManagers;

public class MK4SPrinterConfigManager : DeviceConfigManagerBase<MK4SConfig, IPrusaMK4S>
{
  public MK4SPrinterConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {

  }
}
