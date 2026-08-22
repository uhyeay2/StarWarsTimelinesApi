using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="Character"/> entity, including its optional biographical
/// attributes (birth planet, birth and death year ranges, and species).
/// </summary>
public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("Characters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasOne(x => x.PlanetBornOn)
            .WithMany()
            .HasForeignKey(x => x.PlanetBornOnId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Species)
            .WithMany()
            .HasForeignKey(x => x.SpeciesId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
