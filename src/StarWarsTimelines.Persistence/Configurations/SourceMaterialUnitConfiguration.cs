using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="SourceMaterialUnit"/> entity and its relationships.
/// </summary>
public sealed class SourceMaterialUnitConfiguration : IEntityTypeConfiguration<SourceMaterialUnit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SourceMaterialUnit> builder)
    {
        builder.ToTable("SourceMaterialUnits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Title).HasMaxLength(200);

        // A material can have at most one unit per number within a group (season/volume). The group is null for
        // materials released without groups; SQLite treats nulls as distinct in unique indexes, so the duplicate
        // check is also enforced in the service layer.
        builder.HasIndex(x => new { x.SourceMaterialId, x.GroupNumber, x.Number }).IsUnique();

        builder.HasOne(x => x.SourceMaterial)
            .WithMany(material => material.SourceMaterialUnits)
            .HasForeignKey(x => x.SourceMaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
