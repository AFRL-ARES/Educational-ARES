using System.Collections.ObjectModel;
using Ares.Datamodel.Templates;
using DynamicData;
using ReactiveUI;
using UI.Backend.Extensions;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class PlannableParameterDesignerViewModel : ReactiveObject
{
  private readonly ParameterEditorFactory _editorFactory;
  private IEnumerable<ParameterMetadata> _parameterMetadata = [];
  private ExperimentTemplate? _experimentTemplate;

  public PlannableParameterDesignerViewModel(IEnumerable<ParameterMetadata> existingMetadata, ExperimentTemplate? experimentTemplate, ParameterEditorFactory editorFactory)
  {
    _editorFactory = editorFactory;
    _experimentTemplate = experimentTemplate;
    ParameterMetadata = existingMetadata;
  }

  public ObservableCollection<ParameterEditorViewModel> ParameterEditors { get; } = new();

  public IEnumerable<ParameterMetadata> ParameterMetadata
  {
    private get => _parameterMetadata;

    set
    {
      _parameterMetadata = value;
      Init(value);
    }
  }

  public IEnumerable<ParameterMetadata> Save()
  {
    return ParameterEditors.Select(model => model.Save());
  }

  private void Init(IEnumerable<ParameterMetadata> paramMetadata)
  {
    var outputs = _experimentTemplate?.GetAllOutputCommands().SelectMany(cmd => cmd.UserOutputKeyMap.Select(m => m.Value)).ToArray() ?? [];
    ParameterEditors.AddRange(paramMetadata.Select(metadata => _editorFactory.Create(metadata, outputs)));
  }

  public void Create()
  {
    var outputs = _experimentTemplate?.GetAllOutputCommands().SelectMany(cmd => cmd.UserOutputKeyMap.Select(m => m.Value)).ToArray() ?? [];
    ParameterEditors.Add(_editorFactory.Create(outputs));
  }

  public void Remove(ParameterEditorViewModel vm)
  {
    ParameterEditors.Remove(vm);
  }
}
