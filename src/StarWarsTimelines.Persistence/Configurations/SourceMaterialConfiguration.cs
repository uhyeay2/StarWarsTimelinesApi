using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="SourceMaterial"/> entity.
/// </summary>
public sealed class SourceMaterialConfiguration : IEntityTypeConfiguration<SourceMaterial>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SourceMaterial> builder)
    {
        builder.ToTable("SourceMaterials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Medium).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.CanonType).HasConversion<string>().HasMaxLength(50);
    }
}
