using Ares.Core;
using Ares.Core.Device;
using AresCamera;
using AresCamera.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.ConfigManagers;

public class AresCameraConfigManager : DeviceConfigManagerBase<AresCameraConfig, IAresCamera>
{
  public AresCameraConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {

  }
}
