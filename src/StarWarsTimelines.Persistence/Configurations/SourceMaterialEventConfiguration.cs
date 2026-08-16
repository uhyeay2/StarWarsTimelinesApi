using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="SourceMaterialEvent"/> entity and its relationships.
/// </summary>
public sealed class SourceMaterialEventConfiguration : IEntityTypeConfiguration<SourceMaterialEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SourceMaterialEvent> builder)
    {
        builder.ToTable("SourceMaterialEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.CanonType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.DisplayDate).HasMaxLength(50);
        builder.Property(x => x.DisplayDateEnd).HasMaxLength(50);

        // No inverse navigation on SourceMaterial: the catalog is never queried from the "every event of this
        // material" direction, so keeping the collection off the entity prevents accidental data loads. Cascading
        // mirrors the UserSourceMaterial rule: deleting a source material removes its downstream content.
        builder.HasOne(x => x.SourceMaterial)
            .WithMany()
            .HasForeignKey(x => x.SourceMaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
