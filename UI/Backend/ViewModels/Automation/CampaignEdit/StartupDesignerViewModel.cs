using Ares.Datamodel.Templates;
using Ares.Services;
using Radzen;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public partial class StartupDesignerViewModel : ReactiveObject
{
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;
  private ExperimentTemplate _startupTemplate = null!;
  readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly NotificationService _notificationService;

  public StartupDesignerViewModel(StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
    NotificationService notificationService)
  {
    _stepDesignerFactory = stepDesignerFactory;
    _automationClient = automationClient;
    _validationClient = validationClient;
    _notificationService = notificationService;

    Name = "Unnamed Startup Template";
    StartupTemplate = new ExperimentTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Startup Template"
    };
  }

  public StartupDesignerViewModel(ExperimentTemplate existingTemplate,
  StepDesignerFactory stepDesignerFactory,
  AresAutomation.AresAutomationClient automationClient,
  AresValidation.AresValidationClient validationClient,
  NotificationService notificationService) : this(stepDesignerFactory, automationClient, validationClient, notificationService)
  {
    StartupTemplate = existingTemplate;
  }

  private void Init(ExperimentTemplate existingTemplate)
  {
    Name = existingTemplate.Name;
    StartupStepDesigners = existingTemplate.StepTemplates.Select(template => _stepDesignerFactory.Create(template)).OrderBy(model => model.Index).ToList();
    if(existingTemplate.StepTemplates.Select(step => step.CommandTemplates.Select(cmd => cmd.UserOutputKeyMap)).Any())
    {
      var commandDesigners = StartupStepDesigners.SelectMany(model => model.CommandDesigners).Where(model => model.CommandTemplate.UserOutputKeyMap.Any());
      foreach(var designer in commandDesigners)
      {
        designer.OutputProvider = true;
      }

      ExperimentOutputProviderCommand = commandDesigners.Select(designer => designer.CommandTemplate);
    }
  }

  public ExperimentTemplate Save()
  {
    if(StartupStepDesigners is null)
    {
      _notificationService.Notify(NotificationSeverity.Error, "A Step Designer was null! No data saved.");
      return StartupTemplate;
    }

    StartupTemplate.Name = Name;
    StartupTemplate.StepTemplates.Clear();
    StartupTemplate.StepTemplates.AddRange(StartupStepDesigners.Select(designer => designer.Save()));
    return StartupTemplate;
  }

  public StepDesignerViewModel AddStartupStep()
  {
    var stepDesigner = _stepDesignerFactory.Create();
    stepDesigner.Index = StartupStepDesigners.Count;
    StartupStepDesigners.Add(stepDesigner);
    return stepDesigner;
  }

  public void RemoveStartupStep(StepDesignerViewModel vm)
  {
    if(StartupStepDesigners is not null)
    {
      StartupStepDesigners.Remove(vm);
      ReindexStartupSteps();
    }
  }

  public void MoveStartupStepUp(StepDesignerViewModel vm)
  {
    if(StartupStepDesigners is null || vm.Index == 0)
      return;

    StartupStepDesigners.RemoveAt(vm.Index);
    StartupStepDesigners.Insert(vm.Index - 1, vm);
    ReindexStartupSteps();
  }

  public void MoveStartupStepDown(StepDesignerViewModel vm)
  {
    if(StartupStepDesigners is null || vm.Index == StartupStepDesigners.Count - 1)
      return;

    StartupStepDesigners.RemoveAt(vm.Index);
    StartupStepDesigners.Insert(vm.Index + 1, vm);
    ReindexStartupSteps();
  }

  private void ReindexStartupSteps()
  {
    if(StartupStepDesigners is not null)
    {
      var idx = 0;
      foreach(var startupStep in StartupStepDesigners)
        startupStep.Index = idx++;
    }
  }

  public ExperimentTemplate StartupTemplate
  {
    private get => _startupTemplate;

    set
    {
      _startupTemplate = value;
      Init(value);
    }
  }

  [Reactive]
  public partial string Name { get; set; }

  public IList<StepDesignerViewModel> StartupStepDesigners { get; private set; } = [];

  public IEnumerable<CommandTemplate>? ExperimentOutputProviderCommand { get; set; }
}
