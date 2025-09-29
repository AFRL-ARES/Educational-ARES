using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class DeviceStateEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceState>
{
  public override void Configure(EntityTypeBuilder<DeviceState> builder)
  {
    base.Configure(builder);
    builder.Property(b => b.Data).HasAresStruct();
    builder.Property(b => b.Timestamp).HasTimestamp();
  }
}
