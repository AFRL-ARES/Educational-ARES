using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class CommandDesignerViewModel : ReactiveObject
{
  private readonly CommandParameterDesignerFactory _commandParameterDesignerFactory;
  private readonly MetadataPickerFactory _metadataPickerFactory;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private CommandMetadata? _commandMetadata;
  private CommandTemplate _commandTemplate = null!;

  public CommandDesignerViewModel(
    CommandTemplate existingTemplate,
    CommandParameterDesignerFactory commandParameterDesignerFactory,
    MetadataPickerFactory metadataPickerFactory,
    AresDevices.AresDevicesClient devicesClient)
  {
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _metadataPickerFactory = metadataPickerFactory;
    _devicesClient = devicesClient;

    CommandTemplate = existingTemplate;
  }

  public CommandDesignerViewModel(CommandParameterDesignerFactory commandParameterDesignerFactory, MetadataPickerFactory metadataPickerFactory, AresDevices.AresDevicesClient devicesClient)
  {
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _metadataPickerFactory = metadataPickerFactory;
    _devicesClient = devicesClient;

    CommandTemplate = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString()
    };
  }

  public CommandTemplate CommandTemplate
  {
    get => _commandTemplate;

    set
    {
      _commandTemplate = value;
      CommandMetadata = value.Metadata;
      InitTemplate(value);
    }
  }

  public int Index { get; set; }

  public string? TemplateDeviceName { get; private set; }
  public string? MetadataDeviceName { get; private set; }
  public string? TemplateCommandName => CommandTemplate.Metadata?.Name;

  public bool TemplateOutputProvider => CommandTemplate.UserOutputKeyMap.Any();

  public bool OutputProvider { get; set; }

  public IEnumerable<Parameter> Arguments => CommandTemplate.Parameters;

  public CommandMetadata? CommandMetadata
  {
    get => _commandMetadata;

    set
    {
      _commandMetadata = value;
      InitMetadata(value);
    }
  }

  public UserOutputSelection[] OutputKeyMap { get; private set; } = [];

  public MetadataPickerViewModel? MetadataPickerViewModel { get; set; }

  [Reactive]
  public IEnumerable<CommandParameterDesignerViewModel> ArgumentDesigners { get; private set; } = [];

  public CommandTemplate Save()
  {
    CommandTemplate.Parameters.Clear();
    CommandTemplate.Parameters.AddRange(ArgumentDesigners.Select(model => model.Save()));
    if(CommandMetadata is not null)
    {
      CommandTemplate.Metadata = CommandMetadata;
      CommandTemplate.Metadata.DeviceType = MetadataDeviceName;
    }
      

    CommandTemplate.Index = Index;
    CommandTemplate.UserOutputKeyMap.Clear();
    if(OutputProvider)
    {
      foreach(var selection in OutputKeyMap)
      {
        CommandTemplate.UserOutputKeyMap[selection.DeviceOutputName] = selection.CustomName;
      }
    }
    else
    {
      CommandTemplate.UserOutputKeyMap.Clear();
    }

    return CommandTemplate;
  }

  private async Task InitTemplate(CommandTemplate existingTemplate)
  {
    Index = Convert.ToInt32(existingTemplate.Index);
    var existingParamDesigners = existingTemplate.Parameters.Select(_commandParameterDesignerFactory.Create).ToArray();
    ArgumentDesigners = [.. existingParamDesigners];
    MetadataPickerViewModel = _metadataPickerFactory.Create(existingTemplate.Metadata);

    foreach(var kvp in existingTemplate.UserOutputKeyMap)
    {
      var existingValue = OutputKeyMap.FirstOrDefault(keyValue => keyValue.DeviceOutputName == kvp.Key);
      if(existingValue is null)
      {
        continue;
      }

      existingValue.CustomName = kvp.Value;
    }

    OutputProvider = existingTemplate.UserOutputKeyMap.Any();

    // Revisit this once we have some sort of caching on the UI end.
    // that way we don't have to bother the service every time
    if(existingTemplate.Metadata?.DeviceId is not null)
    {
      var deviceInfo = await _devicesClient.GetDeviceInfoAsync(new DeviceInfoRequest { DeviceId = existingTemplate.Metadata.DeviceId });
      TemplateDeviceName = string.IsNullOrEmpty(deviceInfo.Name) ? null : deviceInfo.Name;
    }
  }

  private async Task InitMetadata(CommandMetadata? existingMetadata)
  {
    ArgumentDesigners = existingMetadata?.ParameterMetadatas.Select(_commandParameterDesignerFactory.Create).ToArray() ?? [];

    var outputs = existingMetadata?.OutputMetadata?.DataSchema;
    if(outputs is not null)
    {
      var newOutputs = outputs.Fields.Where(kvp => OutputKeyMap.All(uos => uos.DeviceOutputName != kvp.Key)).Select(newKvp => new UserOutputSelection(newKvp.Key, newKvp.Value.Type, newKvp.Key));
      var removedOutputs = OutputKeyMap.Where(output => !outputs.Fields.ContainsKey(output.DeviceOutputName));
      OutputKeyMap = [.. OutputKeyMap.Concat(newOutputs).Except(removedOutputs)];
    }
    else
    {
      OutputKeyMap = [];
    }

    if(CommandMetadata?.DeviceId is not null)
    {
      var deviceInfo = await _devicesClient.GetDeviceInfoAsync(new DeviceInfoRequest { DeviceId = CommandMetadata.DeviceId });
      MetadataDeviceName = string.IsNullOrEmpty(deviceInfo.Name) ? null : deviceInfo.Name;
    }
  }
}

public record UserOutputSelection
{
  public UserOutputSelection(string deviceOutputName, AresDataType deviceOutputType, string customName)
  {
    DeviceOutputName = deviceOutputName;
    DeviceOutputType = deviceOutputType;
    CustomName = customName;
  }

  public string DeviceOutputName { get; }

  public AresDataType DeviceOutputType { get; }

  public string CustomName { get; set; }
}
