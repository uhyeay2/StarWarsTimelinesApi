using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="EventLocation"/> link entity.
/// </summary>
public sealed class EventLocationConfiguration : IEntityTypeConfiguration<EventLocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventLocation> builder)
    {
        builder.ToTable("EventLocations");
        builder.HasKey(x => new { x.EventId, x.LocationId });

        builder.HasOne(x => x.SourceMaterialEvent)
            .WithMany(e => e.EventLocations)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // No inverse navigation on Location: the lookup catalog is never queried from the "every event of this
        // location" direction, so WithMany() omits the collection and prevents accidental data loads.
        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
