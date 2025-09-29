using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Humanizer;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.Helpers;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class ParameterEditorViewModel : ReactiveObject
{
  private readonly UnitCategoryHelper _unitHelper;
  private string? _category;
  private double _maximum;
  private double _minimum;
  private string? _name;
  private ParameterMetadata _parameterMetadata = null!;
  private string? _unit;

  public ParameterEditorViewModel(UnitCategoryHelper unitHelper)
  {
    _unitHelper = unitHelper;

    ParameterMetadata = new ParameterMetadata
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Param",
      Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.UnspecifiedType, false),
      Constraints =
      {
        new Limits()
      }
    };
  }

  public ParameterEditorViewModel(ParameterMetadata existingMetadata, UnitCategoryHelper unitHelper)
  {
    _unitHelper = unitHelper;
    ParameterMetadata = existingMetadata;
  }

  public ParameterMetadata ParameterMetadata
  {
    get => _parameterMetadata;

    set
    {
      _parameterMetadata = value;
      Init(value);
    }
  }

  public AresDataType DataType { get; set; }

  public string? Category
  {
    get => _category;

    set
    {
      var changed = _category != value;
      this.RaiseAndSetIfChanged(ref _category, value);
      if(value is null || !changed)
        return;

      UnitOptions = UnitCategoryHelper.GetTypes(value.Dehumanize()).Select(s => s.Humanize()).ToList();

      if(value == "None" || UnitOptions.Count == 0)
        UnitOptions.Add("None");

      Unit = UnitOptions.First();
    }
  }

  [Reactive]
  public List<string> CategoryOptions { get; private set; } = new();

  [Reactive]
  public List<string> UnitOptions { get; private set; } = new();

  public string? Unit
  {
    get => _unit;

    set
    {
      this.RaiseAndSetIfChanged(ref _unit, value);
      if(value == null)
        return;

      Category = _unitHelper.GetCategoryForUnit(_unit.Dehumanize()).Humanize();
    }
  }

  public string? Name
  {
    get => _name;

    set => this.RaiseAndSetIfChanged(ref _name, value);
  }

  public double Minimum
  {
    get => _minimum;

    set => this.RaiseAndSetIfChanged(ref _minimum, value);
  }

  public double Maximum
  {
    get => _maximum;

    set => this.RaiseAndSetIfChanged(ref _maximum, value);
  }

  private void Init(ParameterMetadata meta)
  {
    LockInParams(meta);
    Name = meta.Name;
    DataType = meta.Schema.Type;
    foreach(var metaConstraint in meta.Constraints)
    {
      Minimum = metaConstraint.Minimum;
      Maximum = metaConstraint.Maximum;
    }
  }

  public ParameterMetadata Save()
  {
    if(ParameterMetadata.Constraints.Count == 0)
      ParameterMetadata.Constraints.Add(new Limits());

    ParameterMetadata.Schema = AresSchemaHelper.CreateSchemaEntry(DataType, false);
    ParameterMetadata.Name = Name;

    if(DataType == AresDataType.Number)
    {
      ParameterMetadata.Constraints[0].Maximum = (float)Maximum;
      ParameterMetadata.Constraints[0].Minimum = (float)Minimum;
      ParameterMetadata.Unit = Unit.Dehumanize();
    }

    return ParameterMetadata;
  }

  private void LockInParams(ParameterMetadata meta)
  {
    var categories = _unitHelper.UnitCategories.Select(s => s.Humanize()).ToList();
    categories.Add("None");
    CategoryOptions = categories;
    var unit = meta.Unit;
    var cat = _unitHelper.GetCategoryForUnit(unit);
    UnitOptions = UnitCategoryHelper.GetTypes(cat).Select(s => s.Humanize()).ToList();
    Category = cat.Humanize();
    Unit = unit.Humanize();
  }
}
