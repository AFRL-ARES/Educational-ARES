using System.Reflection;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Device;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core;

public class CoreDatabaseContext : DbContext
{
  public CoreDatabaseContext(DbContextOptions options) : base(options)
  {
  }

  public DbSet<CampaignTemplate> CampaignTemplates => Set<CampaignTemplate>();
  public DbSet<Project> Projects => Set<Project>();
  public DbSet<StepTemplate> StepTemplates => Set<StepTemplate>();
  public DbSet<ExperimentTemplate> ExperimentTemplates => Set<ExperimentTemplate>();
  public DbSet<CommandTemplate> CommandTemplates => Set<CommandTemplate>();
  public DbSet<CampaignExecutionSummary> CampaignExecutionSummaries => Set<CampaignExecutionSummary>();
  public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
  public DbSet<RemoteDeviceConfig> RemoteDeviceConfigs => Set<RemoteDeviceConfig>();
  public DbSet<DeviceSettings> DeviceSettings => Set<DeviceSettings>();

  public DbSet<DeviceInfo> DeviceInfos => Set<DeviceInfo>();
  public DbSet<AnalyzerConfig> Analyzers => Set<AnalyzerConfig>();
  public DbSet<AnalyzerInfo> AnalyzerInfos => Set<AnalyzerInfo>();
  public DbSet<AnalyzerSettings> AnalyzerSettings => Set<AnalyzerSettings>();
  public DbSet<PlannerConfig> Planners => Set<PlannerConfig>();
  public DbSet<PlannerServiceInfo> PlannerInfos => Set<PlannerServiceInfo>();
  public DbSet<PlannerSettings> PlannerSettings => Set<PlannerSettings>();
  public DbSet<AresCampaignTag> CampaignTags => Set<AresCampaignTag>();
  public DbSet<Parameter> Parameters => Set<Parameter>();
  public DbSet<DeviceLoggingSettings> DeviceLoggingSettings => Set<DeviceLoggingSettings>();
  public DbSet<DeviceState> DeviceStates => Set<DeviceState>();


  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var assembly = Assembly.GetAssembly(typeof(CoreDatabaseContext));
    if(assembly is null)
      return;

    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    base.OnModelCreating(modelBuilder);
  }
}
