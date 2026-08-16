using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="UserSourceMaterialUnit"/> entity and its relationships.
/// </summary>
public sealed class UserSourceMaterialUnitConfiguration : IEntityTypeConfiguration<UserSourceMaterialUnit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSourceMaterialUnit> builder)
    {
        builder.ToTable("UserSourceMaterialUnits");
        builder.HasKey(x => new { x.UserId, x.SourceMaterialUnitId });

        builder.HasOne(x => x.User)
            .WithMany(user => user.UserSourceMaterialUnits)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // No inverse navigation on SourceMaterialUnit: progress is never queried from the "every user of this
        // unit" direction, so keeping the collection off the entity prevents accidental data loads.
        builder.HasOne(x => x.SourceMaterialUnit)
            .WithMany()
            .HasForeignKey(x => x.SourceMaterialUnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
