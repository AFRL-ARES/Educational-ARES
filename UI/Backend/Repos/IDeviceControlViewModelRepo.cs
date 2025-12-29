using DynamicData;
using UI.Backend.ViewModels;

namespace UI.Backend.Repos
{
  public interface IDeviceControlViewModelRepo : ISourceList<DeviceUnitControlViewModel>
  {
    void Initialize();
  }
}
