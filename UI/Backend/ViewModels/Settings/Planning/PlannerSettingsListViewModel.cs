using Ares.Datamodel.Planning;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Services.Notification;


namespace UI.Backend.ViewModels.Settings.Planning;

public partial class PlannerSettingsListViewModel : ReactiveObject
{
  private readonly AresPlannerManagementService.AresPlannerManagementServiceClient _planningService;
  private readonly INotificationReceivingService _notificationService;

  public PlannerSettingsListViewModel(AresPlannerManagementService.AresPlannerManagementServiceClient planningService,
    INotificationReceivingService notificationService)
  {
    _planningService = planningService;
    _notificationService = notificationService;
    UpdateAvailablePlanners();
  }

  public PlannerConfigEditViewModel GetNewConfigEditViewModel() => new(_planningService);

  public Task UpdateAvailablePlanners()
  {
    SettingsViewModels = null;
    return _planningService
      .GetAllPlannersAsync(new Empty())
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Planners));
  }

  private void UpdateViewModels(IEnumerable<PlannerServiceInfo> plannerAdapters)
  {
    plannerAdapters = plannerAdapters.Where(planner => planner.Name != "Manual Planner");
    var viewModels = plannerAdapters.Select(info => new PlannerSettingsViewModel(_planningService, _notificationService, info, OnPlannerRemoved)).ToList();
    SettingsViewModels = viewModels;
  }

  public async Task AddNewPlanner(PlannerServiceInfo plannerAdapter)
  {
    var request = new AddPlannerRequest() { Name = plannerAdapter.Name, Address = plannerAdapter.Address };
    await _planningService.AddPlannerAsync(request);
    await UpdateAvailablePlanners();
  }

  private async Task OnPlannerRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailablePlanners();
  }
  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public partial IEnumerable<PlannerSettingsViewModel>? SettingsViewModels { get; private set; }
}
