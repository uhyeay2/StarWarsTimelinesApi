using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="UserSourceMaterial"/> entity and its relationships.
/// </summary>
public sealed class UserSourceMaterialConfiguration : IEntityTypeConfiguration<UserSourceMaterial>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSourceMaterial> builder)
    {
        builder.ToTable("UserSourceMaterials");
        builder.HasKey(x => new { x.UserId, x.SourceMaterialId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);

        builder.HasOne(x => x.User)
            .WithMany(user => user.UserSourceMaterials)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // No inverse navigation on SourceMaterial: the catalog is never queried from the "every user of this
        // material" direction, so keeping the collection off the entity prevents accidental data loads.
        builder.HasOne(x => x.SourceMaterial)
            .WithMany()
            .HasForeignKey(x => x.SourceMaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
