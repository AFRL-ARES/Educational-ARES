using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel.Device;

namespace Ares.Core.Device.State.Export;

public abstract class DeviceStateDataProviderBase<TState> : IDeviceStateDataProvider where TState : class, IDeviceState
{
  readonly IDeviceStateGetter _stateGetter;
  public DeviceStateDataProviderBase(IDeviceStateGetter stateGetter)
  { _stateGetter = stateGetter; }

  public async Task<IEnumerable<SingleDeviceStateExportData>> GetExportData(DeviceStateRequestFilter filter)
  {
    var states = await _stateGetter.GetStates<TState>(filter);
    var exportData = new List<SingleDeviceStateExportData>();
    foreach(var group in states)
    {
      var lines = GetExportLines(group.Key, group.Value).OrderBy(l => l.Timestamp).ToArray();
      var data = new SingleDeviceStateExportData(group.Key, lines);
      exportData.Add(data);
    }

    return exportData;
  }

  protected abstract IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<TState> deviceStates);
}
