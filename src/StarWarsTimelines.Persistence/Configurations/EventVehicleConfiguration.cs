using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="EventVehicle"/> link entity.
/// </summary>
public sealed class EventVehicleConfiguration : IEntityTypeConfiguration<EventVehicle>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventVehicle> builder)
    {
        builder.ToTable("EventVehicles");
        builder.HasKey(x => new { x.EventId, x.VehicleId });

        builder.HasOne(x => x.SourceMaterialEvent)
            .WithMany(e => e.EventVehicles)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // No inverse navigation on Vehicle: the lookup catalog is never queried from the "every event of this
        // vehicle" direction, so WithMany() omits the collection and prevents accidental data loads.
        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
