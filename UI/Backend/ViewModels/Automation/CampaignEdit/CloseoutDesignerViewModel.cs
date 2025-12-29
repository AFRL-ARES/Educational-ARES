using Ares.Datamodel.Templates;
using Ares.Services;
using Radzen;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public partial class CloseoutDesignerViewModel : ReactiveObject
{
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;
  private ExperimentTemplate _closeoutTemplate = null!;
  readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly NotificationService _notificationService;

  public CloseoutDesignerViewModel(StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
    NotificationService notificationService)
  {
    _automationClient = automationClient;
    _stepDesignerFactory = stepDesignerFactory;
    _validationClient = validationClient;
    _notificationService = notificationService;
    Name = "Unnamed Startup Template";
    CloseoutTemplate = new ExperimentTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Closeout Template"
    };
  }

  public CloseoutDesignerViewModel(ExperimentTemplate existingTemplate,
    StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
    NotificationService notificationService) : this(stepDesignerFactory, automationClient, validationClient, notificationService)
  {
    CloseoutTemplate = existingTemplate;
  }

  private void Init(ExperimentTemplate existingTemplate)
  {
    Name = existingTemplate.Name;
    CloseoutStepDesigners = existingTemplate.StepTemplates.Select(template => _stepDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
    if(existingTemplate.StepTemplates.Select(step => step.CommandTemplates.Select(cmd => cmd.UserOutputKeyMap)).Any())
    {
      var commandDesigners = CloseoutStepDesigners.SelectMany(model => model.CommandDesigners).Where(model => model.CommandTemplate.UserOutputKeyMap.Any());
      foreach(var designer in commandDesigners)
      {
        designer.OutputProvider = true;
      }

      ExperimentOutputProviderCommand = commandDesigners.Select(designer => designer.CommandTemplate);
    }
  }

  public ExperimentTemplate Save()
  {
    if(CloseoutStepDesigners is null)
    {
      _notificationService.Notify(NotificationSeverity.Error, "A Step Designer was null! No data saved.");
      return CloseoutTemplate;
    }

    CloseoutTemplate.Name = Name;
    CloseoutTemplate.StepTemplates.Clear();
    CloseoutTemplate.StepTemplates.AddRange(CloseoutStepDesigners.Select(designer => designer.Save()));
    return CloseoutTemplate;
  }

  public void MoveCloseoutStepUp(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is null || vm.Index == 0)
      return;

    CloseoutStepDesigners.RemoveAt(vm.Index);
    CloseoutStepDesigners.Insert(vm.Index - 1, vm);
    ReindexCloseoutSteps();
  }

  public void MoveCloseoutStepDown(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is null || vm.Index == CloseoutStepDesigners.Count - 1)
      return;

    CloseoutStepDesigners.RemoveAt(vm.Index);
    CloseoutStepDesigners.Insert(vm.Index + 1, vm);
    ReindexCloseoutSteps();
  }

  public StepDesignerViewModel AddCloseoutStep()
  {
    var stepDesigner = _stepDesignerFactory.Create();
    stepDesigner.Index = CloseoutStepDesigners.Count;
    CloseoutStepDesigners.Add(stepDesigner);
    return stepDesigner;
  }

  public void RemoveCloseoutStep(StepDesignerViewModel vm)
  {
    if(CloseoutStepDesigners is not null)
    {
      CloseoutStepDesigners.Remove(vm);
      ReindexCloseoutSteps();
    }
  }

  private void ReindexCloseoutSteps()
  {
    if(CloseoutStepDesigners is not null)
    {
      var idx = 0;
      foreach(var closeoutStep in CloseoutStepDesigners)
        closeoutStep.Index = idx++;
    }
  }

  public ExperimentTemplate CloseoutTemplate
  {
    private get => _closeoutTemplate;

    set
    {
      _closeoutTemplate = value;
      Init(value);
    }
  }

  [Reactive]
  public partial string Name { get; set; }
  public IList<StepDesignerViewModel> CloseoutStepDesigners { get; private set; } = [];
  public IEnumerable<CommandTemplate>? ExperimentOutputProviderCommand { get; set; }
}
