using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using MK4S.Config;
using MK4S.Services;
using PrusaMK4S;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Settings.Device.PrusaMK4S;

public class PrusaMK4SSettingsListViewModel : ReactiveObject
{
  private readonly MK4SPrinterRpc.MK4SPrinterRpcClient _client;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;

  public PrusaMK4SSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, MK4SPrinterRpc.MK4SPrinterRpcClient printerClient, INotificationReceivingService notificationService)
  {
    _client = printerClient;
    _devicesClient = devicesClient;
    _notificationService = notificationService;
    UpdateConfigs();
  }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new PrusaMK4SSettingsViewModel(_client, config, _notificationService, _devicesClient, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public PrusaMK4SConfigEditViewModel GetNewConfigEditViewModel() => new(_client, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IPrusaMK4S).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(MK4SConfig config)
  {
    await _client.AddMK4SPrinterAsync(config);
    await UpdateConfigs();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public IEnumerable<PrusaMK4SSettingsViewModel>? SettingsViewModels { get; private set; }
}