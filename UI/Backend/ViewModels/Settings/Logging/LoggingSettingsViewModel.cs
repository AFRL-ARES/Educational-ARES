using System.Reactive;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Logging;

public class LoggingSettingsViewModel : ReactiveObject
{
  private readonly string _deviceId;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly ObservableAsPropertyHelper<bool> _updatedObservable;

  public LoggingSettingsViewModel(string deviceId, string deviceName, AresDevices.AresDevicesClient devicesClient)
  {
    _deviceId = deviceId;
    DeviceName = deviceName;
    _devicesClient = devicesClient;

    this.WhenAnyValue(vm => vm.IntervalMs,
      vm => vm.LoggingType,
      vm => vm.CurrentSettings,
      vm => vm.DeltasChanged,
      (interval, logType, settings, deltasChanged) =>
        (settings is not null && (interval != settings.IntervalMs || logType != settings.LoggingType)) || deltasChanged
    ).ToProperty(this, vm => vm.Updated, out _updatedObservable);

    FetchSettingsCommand = ReactiveCommand.CreateFromTask(Init);
  }
  public string DeviceName { get; }

  [Reactive]
  public bool Fetched { get; private set; }

  [Reactive]
  public DeviceLoggingSettings.Types.LoggingType LoggingType { get; set; }

  [Reactive]
  private DeviceLoggingSettings? CurrentSettings { get; set; }

  [Reactive]
  private bool DeltasChanged { get; set; }

  [Reactive]
  public long IntervalMs { get; set; }

  public Dictionary<string, double> Deltas { get; } = [];

  public bool Updated => _updatedObservable.Value;

  public ReactiveCommand<Unit, Unit> FetchSettingsCommand { get; }

  public void UpdateDelta(string key, double delta)
  {
    var currentDelta = CurrentSettings?.Deltas.GetValueOrDefault(key);
    if(currentDelta is null || currentDelta == delta)
    {
      return;
    }

    Deltas[key] = delta;
    DeltasChanged = AnyDeltasChanged();
  }

  private bool AnyDeltasChanged()
  {
    if(CurrentSettings is null)
      return false;

    foreach(var item in Deltas)
    {
      var existingDelta = CurrentSettings.Deltas.GetValueOrDefault(item.Key);

      if(item.Value != existingDelta)
        return true;
    }

    return false;
  }

  public async Task Init()
  {
    Fetched = false;
    var settings = await _devicesClient.GetDeviceLoggerSettingsAsync(new DeviceLoggerSettingsRequest { DeviceId = _deviceId });

    CurrentSettings = settings;
    IntervalMs = settings.IntervalMs;
    LoggingType = settings.LoggingType;

    var stateSchema = await _devicesClient.GetDeviceStateSchemaAsync(new DeviceStateSchemaRequest { DeviceId = _deviceId });
    var numericFields = stateSchema.Schema?.Fields.Where(f => f.Value.Type == Ares.Datamodel.AresDataType.Number).ToArray() ?? [];

    var deviceDefaultDeltas = numericFields.Select(nf => new KeyValuePair<string, double>(nf.Key, 0)).ToDictionary();
    foreach(var delta in deviceDefaultDeltas)
    {
      var hasSetting = settings.Deltas.TryGetValue(delta.Key, out var deltaSetting);
      if(!hasSetting)
      {
        continue;
      }

      deviceDefaultDeltas[delta.Key] = deltaSetting;
    }

    Deltas.Clear();
    foreach(var item in deviceDefaultDeltas)
    {
      Deltas[item.Key] = item.Value;
    }

    DeltasChanged = AnyDeltasChanged();
    Fetched = true;
  }

  public async Task<bool> Save()
  {
    if(!Updated)
    {
      return false;
    }

    var settings = new DeviceLoggingSettings
    {
      DeviceId = _deviceId,
      IntervalMs = IntervalMs,
      LoggingType = LoggingType,
    };
    settings.Deltas.Add(Deltas);
    await _devicesClient.SetDeviceLoggerSettingsAsync(settings);
    await Init();
    return true;
  }
}
