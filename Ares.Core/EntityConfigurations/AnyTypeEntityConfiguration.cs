using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class AnyTypeEntityConfiguration : AresEntityTypeBaseConfiguration<Any>
{
  public override void Configure(EntityTypeBuilder<Any> builder)
  {
    base.Configure(builder);

    builder.Property(any => any.Value)
      .HasConversion(
        s => s.ToByteArray(),
        bytes => ByteString.CopyFrom(bytes), new EnumerableOfByteComparer());
  }
}
