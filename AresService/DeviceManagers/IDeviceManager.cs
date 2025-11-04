using System.Collections.Generic;
using System.Threading.Tasks;
using Ares.Device;
using AresService.DeviceDbLoaders;
using Google.Protobuf;

namespace AresService.DeviceManagers;

/// <summary>
/// </summary>
/// <typeparam name="TConfig">Config type used for loading the device</typeparam>
public interface IDeviceManager<TConfig, TDevice> where TDevice : IAresDevice where TConfig : IMessage, new()
{
  Task<TDevice> Create(TConfig config);
  Task<TDevice> Load(string deviceId, TConfig config);
  Task<TDevice[]> Load(IEnumerable<LoadableConfig<TConfig>> configs);
  Task<TDevice> Update(string deviceId, TConfig config);
  Task Remove(string deviceId);
}
