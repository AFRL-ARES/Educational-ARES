using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public partial class PlannerAllocationEditorViewModel : ReactiveObject
{
  private readonly AresPlannerManagementService.AresPlannerManagementServiceClient _plannerClient;
  private readonly INotificationReceivingService _notificationService;
  private PlannerServiceInfo? _selectedAdapter;

  public PlannerAllocationEditorViewModel(ParameterMetadata metadata,
    PlannerServiceInfo? plannerInfo,
    IEnumerable<PlannerServiceInfo> plannerAdapters,
    AresPlannerManagementService.AresPlannerManagementServiceClient plannerClient,
    INotificationReceivingService notificationService)
  {
    var plannerArray = plannerAdapters.ToArray();

    ParameterMetadata = metadata;
    PlannerServices = plannerArray;
    _plannerClient = plannerClient;
    _notificationService = notificationService;

    if(plannerInfo is not null)
    {
      SelectedService = plannerInfo;
      SelectedPlannerOption = plannerInfo.Capabilities.AvailablePlanners.FirstOrDefault(p => p.PlannerName == metadata.PlannerName);
    }

    PlannerOptions = Enumerable.Empty<Planner>();
  }

  public PlannerAllocation? Save()
  {
    if(SelectedService is null)
      return null;

    var allocation = new PlannerAllocation
    {
      Parameter = ParameterMetadata,
      Planner = SelectedService,
      UniqueId = Guid.NewGuid().ToString()
    };

    allocation.Parameter.PlannerName = SelectedPlannerOption?.PlannerName ?? SelectedService.Name;
    allocation.Parameter.PlannerDescription = SelectedPlannerOption?.Description ?? SelectedService.Description;

    return allocation;
  }

  public async Task UpdatePlannerOptions()
  {
    if(SelectedService is null)
      return;

    var updatedInfo = await _plannerClient.GetInfoAsync(new PlannerInfoRequest { PlannerId = SelectedService.UniqueId });

    if(updatedInfo.Info.Name != string.Empty)
    {
      PlannerOptions = updatedInfo.Info.Capabilities.AvailablePlanners;

      //Not all adapters will have multiple options, if not auto assign the value
      if(PlannerOptions.Count() == 1)
        SelectedPlannerOption = SelectedService.Capabilities.AvailablePlanners.First();  
    }

    else
    {
      var notification = new AresNotification();
      notification.Title = "Assigned Planner Unavailable!";
      notification.Message = $"This template uses a planner that ARES no longer has a connection with. The template won't be usable until this is resolved.";
      notification.NotificationSeverity = Severity.Warning;
      notification.Loiter = true;
      _notificationService.PushNotification(notification);
    }
  }

  public PlannerServiceInfo? SelectedService
  {
    get => _selectedAdapter;

    set
    {
      if(value is null || _selectedAdapter == value)
        return;

      if(_selectedAdapter is not null)
        SelectedPlannerOption = _selectedAdapter.Capabilities.AvailablePlanners.FirstOrDefault();

      _selectedAdapter = value;
      _ = UpdatePlannerOptions();
    }
  }

  [Reactive]
  public partial ParameterMetadata ParameterMetadata { get; set; }
  public IEnumerable<PlannerServiceInfo> PlannerServices { get; }
  [Reactive]
  public partial IEnumerable<Planner> PlannerOptions { get; set; }
  [Reactive]
  public partial Planner? SelectedPlannerOption { get; set; }
}
