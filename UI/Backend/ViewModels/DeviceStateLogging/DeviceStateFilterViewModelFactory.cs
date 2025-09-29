using Ares.Services;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class DeviceStateFilterViewModelFactory
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  readonly ICombinedDeviceGetter _deviceGetter;

  public DeviceStateFilterViewModelFactory(AresAutomation.AresAutomationClient automationClient, ICombinedDeviceGetter deviceGetter)
  {
    _deviceGetter = deviceGetter;
    _automationClient = automationClient;
  }

  public DeviceStateFilterViewModel Create() => new DeviceStateFilterViewModel(_automationClient, _deviceGetter);
}
