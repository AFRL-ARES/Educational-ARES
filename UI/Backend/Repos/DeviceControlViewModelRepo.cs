using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using UI.Backend.Devices;
using UI.Backend.ViewModels;

namespace UI.Backend.Repos
{
  public class DeviceControlViewModelRepo : IDeviceControlViewModelRepo
  {
    private readonly IMessenger _messenger;

    public DeviceControlViewModelRepo(IMessenger messenger)
    {
      _messenger = messenger;
    }

    public void Initialize()
    {
      _messenger.Register<DeviceDeletedMessage>(this, (recipient, msg) =>
      {
        var viewModelToRemove = _deviceViewModelList.Items.FirstOrDefault(vm => vm.DeviceId == msg.DeviceId);

        if(viewModelToRemove is not null)
          _deviceViewModelList.Remove(viewModelToRemove);
      });
    }

    private SourceList<DeviceUnitControlViewModel> _deviceViewModelList = new();
    public int Count => _deviceViewModelList.Count;

    public IObservable<IChangeSet<DeviceUnitControlViewModel>> Connect(Func<DeviceUnitControlViewModel, bool>? predicate = null)
    {
      return _deviceViewModelList.Connect(predicate);
    }

    public void Dispose()
    {
      _deviceViewModelList?.Dispose();
    }

    public void Edit(Action<IExtendedList<DeviceUnitControlViewModel>> updateAction)
    {
      _deviceViewModelList.Edit(updateAction);
    }

    public IObservable<IChangeSet<DeviceUnitControlViewModel>> Preview(Func<DeviceUnitControlViewModel, bool>? predicate = null)
    {
      return _deviceViewModelList.Preview(predicate);
    }

    public IObservable<int> CountChanged => _deviceViewModelList.CountChanged;

    public IEnumerable<DeviceUnitControlViewModel> Items => _deviceViewModelList.Items;

    IReadOnlyList<DeviceUnitControlViewModel> IObservableList<DeviceUnitControlViewModel  >.Items => _deviceViewModelList.Items;
  }
}
