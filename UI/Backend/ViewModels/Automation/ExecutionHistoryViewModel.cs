using Ares.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Automation;

public partial class ExecutionHistoryViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;

  public ExecutionHistoryViewModel(AresAutomation.AresAutomationClient automationClient)
  {
    _automationClient = automationClient;
    CampaignSummaries = [];
    LoadingExecutionHistory = false;
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
  public partial IList<CampaignExecutionSummaryMetadata> CampaignSummaries { get; set; }

  [Reactive]
  public partial bool LoadingExecutionHistory { get; set; }
}