namespace Ares.Device.Tests.Device;

public class TestDevice : AresDevice
{

  public TestDevice() : base("Test Device", "TestDevice")
  {
  }

  public override Task EnterSafeMode(CancellationToken ct)
    => Task.CompletedTask;

  public override Task<bool> Activate(CancellationToken ct)
    => Task.FromResult(true);
}
