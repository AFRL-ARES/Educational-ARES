using Ares.Datamodel.Device;
using Ares.Device;

namespace Ares.Core.CoreDevice;

public class AresCoreDevice : AresDevice
{
  public AresCoreDevice() : base("ARES", "ARES-CORE-DEVICE")
  {
    Status = new DeviceOperationalStatus()
    {
      OperationalState = OperationalState.Active
    };
  }

  public override Task<bool> Activate(CancellationToken ct)
  {
    return Task.FromResult(true);
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public Task Sleep(TimeSpan timeSpan, CancellationToken ct)
  {
    return Task.Delay(timeSpan, ct);
  }
}
