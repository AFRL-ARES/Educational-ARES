using Ares.Core.Device;
using AresService.DeviceDbLoaders;
using MK4S.Config;
using PrusaMK4S;
using PrusaMK4S.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.DeviceManagers;

public class PrusaMK4SPrinterDeviceManager : IDeviceManager<MK4SConfig, IPrusaMK4S>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public PrusaMK4SPrinterDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public Task<IPrusaMK4S> Create(MK4SConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public Task<IPrusaMK4S> Load(string id, MK4SConfig config)
  {
    IPrusaMK4S printer;

    if(config.Simulated)
      printer = new SimPrusaMK4S(config.DeviceName) { UniqueId = id };

    else
      printer = new PrusaMK4s(config.DeviceName) { UniqueId = id };

    return Task.FromResult(printer);
  }

  public async Task<IPrusaMK4S[]> Load(IEnumerable<LoadableConfig<MK4SConfig>> loadableConfigs)
  {
    var cameras = await Task.WhenAll(loadableConfigs.Select(cfg => Load(cfg.Id, cfg.DeviceConfig)));
    return cameras;
  }

  public async Task Remove(string managerId)
  {
    var printerInterpreter = _deviceCommandInterpreterRepo
  .FirstOrDefault(interpreter => interpreter.Device.UniqueId == managerId);

    if(printerInterpreter?.Device is not IPrusaMK4S printer)
      return;

    await printer.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(printerInterpreter);
  }

  public async Task<IPrusaMK4S> Update(string deviceId, MK4SConfig config)
  {
    var existingPrinter = _deviceCommandInterpreterRepo
    .Select(interpreter => interpreter.Device)
    .OfType<IPrusaMK4S>()
    .FirstOrDefault(device => device.UniqueId == deviceId);

    if(existingPrinter is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingPrinter.Name == config.DeviceName)
      return existingPrinter;

    await Remove(existingPrinter.UniqueId);

    return await Load(existingPrinter.UniqueId, config);
  }


}
