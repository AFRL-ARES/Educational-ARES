using Ares.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Automation;

public class ExecutionHistoryViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;

  public ExecutionHistoryViewModel(AresAutomation.AresAutomationClient automationClient)
  {
    _automationClient = automationClient;
  }

  public async Task UpdateExecutionSummaries()
  {
    LoadingExecutionHistory = true;
    CampaignSummaries.Clear();
    var response = await _automationClient.GetAvailableCampaignExecutionSummariesAsync(new Empty());
    CampaignSummaries.AddRange(response.AvailableCampaignSummaries);
    LoadingExecutionHistory = false;
  }

  [Reactive]
  public IList<CampaignExecutionSummaryMetadata> CampaignSummaries { get; set; } = [];

  [Reactive]
  public bool LoadingExecutionHistory { get; set; } = false;
}