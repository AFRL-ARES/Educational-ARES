using Ares.Core.Device;
using AresCamera;
using AresCamera.Config;
using AresService.DeviceDbLoaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.DeviceManagers;

public class AresCameraDeviceManager : IDeviceManager<AresCameraConfig, IAresCamera>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public AresCameraDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public Task<IAresCamera> Create(AresCameraConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IAresCamera> Load(string id, AresCameraConfig config)
  {
    IAresCamera camera = new AresCamera.AresCamera(config.DeviceName, config.SourceName) { UniqueId = id };
    await camera.Activate();
    var interepreter = new AresCameraInterpreter(camera);
    _deviceCommandInterpreterRepo.Add(interepreter);

    return camera;
  }

  public async Task<IAresCamera[]> Load(IEnumerable<LoadableConfig<AresCameraConfig>> loadableConfigs)
  {
    var cameras = await Task.WhenAll(loadableConfigs.Select(cfg => Load(cfg.Id, cfg.DeviceConfig)));
    return cameras;
  }

  public async Task Remove(string managerId)
  {
    var cameraInterpreter = _deviceCommandInterpreterRepo
  .FirstOrDefault(interpreter => interpreter.Device.UniqueId == managerId);

    if(cameraInterpreter?.Device is not IAresCamera cm3Camera)
      return;

    await cm3Camera.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(cameraInterpreter);
  }

  public async Task<IAresCamera> Update(string deviceId, AresCameraConfig config)
  {
    var existingCamera = _deviceCommandInterpreterRepo
    .Select(interpreter => interpreter.Device)
    .OfType<IAresCamera>()
    .FirstOrDefault(device => device.UniqueId == deviceId);

    if(existingCamera is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingCamera.Name == config.DeviceName)
      return existingCamera;

    await Remove(existingCamera.UniqueId);

    return await Load(existingCamera.UniqueId, config);
  }
}
