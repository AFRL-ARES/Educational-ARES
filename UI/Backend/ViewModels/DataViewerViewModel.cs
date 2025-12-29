using Ares.Datamodel;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Radzen;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Backend.Helpers;

namespace UI.Backend.ViewModels;

public partial class DataViewerViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;

  public DataViewerViewModel(AresAutomation.AresAutomationClient automationClient)
  {
    _automationClient = automationClient;
    SelectedSummaryStartTime = string.Empty;
    SelectedSummaryFinishTime = string.Empty;
    SelectedSummaryNumberOfExperiments = string.Empty;
    SelectedSummaryTags = string.Empty;
    SelectedSummaryNotes = string.Empty;
    AvailableSummaries = Enumerable.Empty<CampaignExecutionSummaryMetadata>();
    LoadingAvailableSumarries = false;
  }

  public async Task GetSelectedSummary()
  {
    if(SelectedSummaryMetadata is null)
      return;

    var fullSummary = await _automationClient.GetCampaignSummaryAsync(new CampaignExecutionSummaryRequest { SummaryId = SelectedSummaryMetadata.SummaryId });

    if(fullSummary is null)
      throw new InvalidOperationException("Campaign Summary Request returned null!");

    FullSelectedSummary = fullSummary;
    SelectedSummaryStartTime = fullSummary.ExecutionInfo.TimeStarted.ToReadableTimestamp();
    SelectedSummaryFinishTime = fullSummary.ExecutionInfo.TimeFinished.ToReadableTimestamp();
    SelectedSummaryNumberOfExperiments = fullSummary.ExperimentSummaries.Count.ToString();
    SelectedSummaryTags = string.Join(" ", fullSummary.CampaignTags);
    SelectedSummaryNotes = fullSummary.CampaignNotes;
  }

  public async Task UpdateAvailableSummaries()
  {
    LoadingAvailableSumarries = true;
    var summaries = await _automationClient.GetAvailableCampaignExecutionSummariesAsync(new Empty());
    AvailableSummaries = summaries.AvailableCampaignSummaries;
    LoadingAvailableSumarries = false;
  }

  public async Task GetSummaryFromLinkId(string id)
  {
    await UpdateAvailableSummaries();
    SelectedSummaryMetadata = AvailableSummaries.FirstOrDefault(sum => sum.SummaryId == id);
    await GetSelectedSummary();
  }

  public void SelectedTreeItemUpdated(TreeEventArgs args)
  {
    SelectedExperimentSummary = null;
    SelectedStepSummary = null;
    SelectedCommandSummary = null;

    if(args.Value is null)
      return;

    if(args.Value is ExperimentExecutionSummary expSummary)
      SelectedExperimentSummary = expSummary;

    else if(args.Value is StepExecutionSummary stepSummary)
      SelectedStepSummary = stepSummary;

    else if(args.Value is CommandExecutionSummary commandExecutionSummary)
      SelectedCommandSummary = commandExecutionSummary;
  }

  [Reactive]
  public partial CampaignExecutionSummary? FullSelectedSummary { get; set; }

  [Reactive]
  public partial string SelectedSummaryStartTime { get; set; }

  [Reactive]
  public partial string SelectedSummaryFinishTime { get; set; }

  [Reactive]
  public partial string SelectedSummaryNumberOfExperiments { get; set; }

  [Reactive]
  public partial string SelectedSummaryTags { get; set; }

  [Reactive]
  public partial string SelectedSummaryNotes { get; set; }

  [Reactive]
  public partial ExperimentExecutionSummary? SelectedExperimentSummary { get; set; }

  [Reactive]
  public partial StepExecutionSummary? SelectedStepSummary { get; set; }

  [Reactive]
  public partial CommandExecutionSummary? SelectedCommandSummary { get; set; }

  [Reactive]
  public partial IEnumerable<CampaignExecutionSummaryMetadata> AvailableSummaries { get; set; }

  [Reactive]
  public partial bool LoadingAvailableSumarries { get; set; }

  public CampaignExecutionSummaryMetadata? SelectedSummaryMetadata { get; set; }
}
