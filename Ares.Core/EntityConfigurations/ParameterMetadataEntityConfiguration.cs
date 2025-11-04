using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class ParameterMetadataEntityConfiguration : AresEntityTypeBaseConfiguration<ParameterMetadata>
{
  public override void Configure(EntityTypeBuilder<ParameterMetadata> builder)
  {
    base.Configure(builder);

    builder.HasMany(parameterMetadata => parameterMetadata.Constraints)
      .WithOne()
      .IsRequired();

    builder.Property(parameterMetadata => parameterMetadata.InitialValue).HasAresValue();

    builder.Property(parameterMetadata => parameterMetadata.Schema)
      .HasAresSchemaEntry();

    builder.Navigation(parameterMetadata => parameterMetadata.Constraints)
      .AutoInclude();
  }
}
