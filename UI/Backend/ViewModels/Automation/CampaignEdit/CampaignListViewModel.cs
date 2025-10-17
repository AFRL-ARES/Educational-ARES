using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using Ares.Datamodel.Templates;
using Ares.Services;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class CampaignListViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;

  public readonly ObservableCollection<CampaignTemplateSummary> Templates = new();

  public CampaignListViewModel(AresAutomation.AresAutomationClient automationClient, IConfiguration configuration)
  {
    _automationClient = automationClient;
  }

  public async Task<CampaignTemplate?> GetFullCampaignTemplate(string campaignId)
  {
    return await _automationClient.GetSingleCampaignAsync(new CampaignRequest { UniqueId = campaignId });
  }

  public async Task RefreshCampaigns()
  {
    var campaigns = await _automationClient.GetAllCampaignsAsync(new GetAllCampaignsRequest());
    Templates.Clear();
    Templates.AddRange(campaigns.Campaigns);
  }

  public async Task DeleteCampaign(Guid campaignId)
  {
    await _automationClient.RemoveCampaignAsync(new CampaignRequest { UniqueId = campaignId.ToString() });
    await RefreshCampaigns();
  }
}
