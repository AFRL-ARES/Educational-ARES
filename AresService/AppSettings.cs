using System;

namespace AresService;

public class AppSettings
{
  public TokensConfig? TokensConfig { get; set; }
  public string AresDataPath { get; set; } = Environment.CurrentDirectory;
  public string DatabaseProvider { get; set; } = string.Empty;
  public static string ResultsFolder = "CampaignResults";

  public static string TemplatesFolder = "CampaignTemplates";

  public static string DevicesFolder = "Devices";

  public static string ExperimentTagsFile = "UserTags.txt";
}
public class TokensConfig
{
  public string? Issuer { get; set; }
  public string? Audience { get; set; }
  public string? Key { get; set; }
}
