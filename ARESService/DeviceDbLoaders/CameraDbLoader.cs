using AresCamera;
using AresCamera.Config;
using AresService;
using AresService.DeviceDbLoaders;
using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceDbLoaders;

public class CameraDbLoader : DeviceDbLoaderBase<IAresCamera, AresCameraConfig>
{
  public CameraDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<AresCameraConfig, IAresCamera> deviceManager) : base(dbContextFactory, deviceManager)
  {

  }
}
