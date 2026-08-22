using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="Species"/> entity.
/// </summary>
public sealed class SpeciesConfiguration : IEntityTypeConfiguration<Species>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Species> builder)
    {
        builder.ToTable("Species");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasOne(x => x.HomePlanet)
            .WithMany()
            .HasForeignKey(x => x.HomePlanetId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
