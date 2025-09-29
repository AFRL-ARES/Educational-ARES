using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class DeviceLoggingSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceLoggingSettings>
{
  public override void Configure(EntityTypeBuilder<DeviceLoggingSettings> builder)
  {
    base.Configure(builder);
    // We're not going to associate the DeviceId as the foreign key for now as the devices are added
    // to the database by different dotnet projects

    builder.Property(b => b.Deltas).HasSerializedMap();
  }
}
