using System.Reactive;
using System.Reactive.Linq;
using Ares.Services;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.ViewModels.DeviceStateLogging;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Settings.Logging;

public class LoggingSettingsListViewModel : ReactiveObject
{
  private readonly ICombinedDeviceGetter _deviceGetter;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;
  private ObservableAsPropertyHelper<bool> _updated;

  public LoggingSettingsListViewModel(ICombinedDeviceGetter deviceGetter, AresDevices.AresDevicesClient devicesClient, INotificationReceivingService notificationService)
  {
    _deviceGetter = deviceGetter;
    _devicesClient = devicesClient;
    _notificationService = notificationService;
    RefreshLoggers = ReactiveCommand.CreateFromTask(_ => FetchLoggers());

    this.WhenAnyValue(x => x.LoggingSettingsViewModels)
      .SelectMany(vms => vms?.Select(vm => vm.WhenAnyValue(x => x.Updated)) ?? [])
      .Merge()
      .Select(_ => LoggingSettingsViewModels?.Any(vm => vm.Updated) ?? false)
      .ToProperty(this, vm => vm.Updated, out _updated);
  }

  public async Task FetchLoggers()
  {
    var devices = await _deviceGetter.GetAvailableDevices();
    LoggingSettingsViewModels = devices.Select(d => new LoggingSettingsViewModel(d.DeviceId, d.DeviceName, _devicesClient)).ToArray();
  }

  [Reactive]
  public LoggingSettingsViewModel[]? LoggingSettingsViewModels { get; private set; }

  public ReactiveCommand<Unit, Unit> RefreshLoggers { get; }

  public bool Updated => _updated.Value;

  public async Task Save()
  {
    if(LoggingSettingsViewModels is null)
    {
      return;
    }

    var saved = await Task.WhenAll(LoggingSettingsViewModels.Select(lsvm => lsvm.Save()));
    if(saved.Any(s => s))
    {
      var notif = new AresNotification
      {
        Message = "Logging updates saved successfully",
        Title = "Logging Settings",
        NotificationSeverity = Severity.Success
      };
      _notificationService.PushNotification(notif);
    }
  }
}