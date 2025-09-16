using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using ReactiveUI;
using UI.Backend.Helpers;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class CommandParameterDesignerViewModel : ReactiveObject
{
  private readonly IEnumerable<ParameterMetadata>? _plannedParameters;
  private readonly UnitCategoryHelper _unitCategoryHelper;
  private bool _isPlanned;
  private Parameter _parameter = null!;
  private bool _valid;
  private AresValue? _value;

  public CommandParameterDesignerViewModel(Parameter param, UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters = null)
    : this(unitCategoryHelper, plannedParameters)
  {
    Parameter = param;
    IsEnvironmentBased = param.EnvironmentBased;
    SelectedVariableType = param.VariableType;
  }

  public CommandParameterDesignerViewModel(ParameterMetadata meta, UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters = null)
    : this(unitCategoryHelper, plannedParameters)
  {
    Parameter = new Parameter
    {
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = meta
    };

    
    Value = AresValueHelper.CreateDefault(meta.Schema.Type);
  }

  private CommandParameterDesignerViewModel(UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters)
  {
    _unitCategoryHelper = unitCategoryHelper;
    _plannedParameters = plannedParameters;
  }

  public Parameter Parameter
  {
    get => _parameter;

    set
    {
      _parameter = value;
      Init(value);
    }
  }

  public string Name => Parameter.Metadata.Name;

  public string Unit => Parameter.Metadata.Unit;

  public SchemaEntry Schema => Parameter.Metadata.Schema;

  public AresValue? Value
  {
    get => _value;

    set
    {
      this.RaiseAndSetIfChanged(ref _value, value);
      if(value is null)
        return;

      Valid = IsValid(value);
    }
  }

  public bool Valid
  {
    get => _valid;

    private set
    {
      _valid = value;
      this.RaisePropertyChanged();
    }
  }

  public IEnumerable<ParameterMetadata> PlannedParameters { get; private set; } = Array.Empty<ParameterMetadata>();

  public string? SelectedPlannedParameterMetadataId { get; set; }

  public VariableType? SelectedVariableType { get; set; }

  public VariableType[] VariableTypes { get; private set; } = System.Enum.GetValues<VariableType>().Skip(1).ToArray();

  public bool IsPlanned
  {
    get => _isPlanned;

    set
    {
      _isPlanned = value;
      Value = value ? null : Parameter.Value ?? new AresValue();
    }
  }

  public bool IsEnvironmentBased { get; set; }

  public int PastExperimentNumber { get; set; }

  private IEnumerable<ParameterMetadata> FilterParameterMetadata(UnitCategoryHelper helper, IEnumerable<ParameterMetadata>? allMetadata)
  {
    if(allMetadata is null)
      return Array.Empty<ParameterMetadata>();

    if(string.IsNullOrEmpty(Unit))
      return allMetadata;

    return allMetadata;
  }

  private void Init(Parameter existingParameter)
  {
    Value = existingParameter.Value;
    IsPlanned = existingParameter.Planned;
    SelectedPlannedParameterMetadataId = existingParameter.PlanningMetadata?.UniqueId;
    PlannedParameters = FilterParameterMetadata(_unitCategoryHelper, _plannedParameters);
    PastExperimentNumber = DeterminePastExperimentNumber(existingParameter.VariableArgument);
  }

  private int DeterminePastExperimentNumber(string arg)
  {
    var parsed = int.TryParse(arg, out var intValue);

    if(parsed)
      return intValue;

    return 0;
  }

  public Parameter Save()
  {
    Parameter.Value = Value;
    Parameter.Planned = IsPlanned;
    Parameter.EnvironmentBased = IsEnvironmentBased;
    Parameter.VariableArgument = PastExperimentNumber.ToString();
    Parameter.VariableType = SelectedVariableType ?? VariableType.VarUnspecified;
    Parameter.PlanningMetadata = Parameter.Planned ? 
      PlannedParameters.FirstOrDefault(metadata => metadata.UniqueId == SelectedPlannedParameterMetadataId) : null;

    return Parameter;
  }

  private bool IsValid(AresValue value)
  {
    switch(Schema.Type)
    {
      case AresDataType.Number:
        if(!value.HasNumberValue)
          return false;

        if(Parameter.Metadata.Constraints.Count == 0)
          return true;

        return Parameter.Metadata.Constraints.Any(limits => value.NumberValue >= limits.Minimum && value.NumberValue <= limits.Maximum);

      case AresDataType.String:
        return value.HasStringValue;

      default:
        return true;
    }
  }
}
