using Ares.Core.Device.Helpers;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel.Device;

namespace Ares.Core.Device.Remote.State;
public class RemoteDeviceExportDataProvider : DeviceStateDataProviderBase<DeviceState>
{
  public RemoteDeviceExportDataProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<DeviceState> deviceStates)
  {
    var exportItems = deviceStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = d.Data.Fields
          .Select(
            f =>
              {
                return new StateExportItem(f.Key, deviceName, f.Value.GetValueAsString());
              });

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });

    return exportItems;
  }
}
