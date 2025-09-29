using Ares.Datamodel.Device;
using CsvHelper.Configuration;

namespace Ares.Core.Device.Remote.State;
public class RemoteDeviceStateMap : ClassMap<DeviceState>
{
  public RemoteDeviceStateMap()
  {
    Map(s => s.DeviceId).Index(0).Name("Device Id");
    Map(s => s.Timestamp).Index(1).Name("Timestamp");

    References<AresStructDataMap>(m => m.Data);
  }
}
