using UI.Pages.Shared.CampaignEdit.CustomCommands.PrusaMK4S;

namespace UI.Services.DynamicViews;

public static class DynamicViewManager
{
  public static bool TryRetrieveCustomView(string commandName, out Type? customView)
  {
    var result = CustomViewMapping.TryGetValue(commandName, out var customViewMapping);

    if(result && customViewMapping is not null)
    {
      customView = customViewMapping;
      return true;
    }

    customView = null;
    return false;
  }
  public static IDictionary<string, Type> CustomViewMapping { get; } = new Dictionary<string, Type>()
  {
    ["GCode"] = typeof(PrusaPrintCommand)
  };
}