using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using MK4S.Config;
using PrusaMK4S;

namespace AresService.DeviceDbLoaders;

public class PrusaMK4SPrinterDbLoader : DeviceDbLoaderBase<IPrusaMK4S, MK4SConfig>
{
  public PrusaMK4SPrinterDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<MK4SConfig, IPrusaMK4S> deviceManager) : base(dbContextFactory, deviceManager)
  {

  }
}
