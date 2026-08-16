namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents the many-to-many link between a <see cref="SourceMaterialEvent"/> and a <see cref="Vehicle"/>.
/// </summary>
/// <remarks>
/// The entity is identified by the composite key (<see cref="EventId"/>, <see cref="VehicleId"/>) so a vehicle can
/// be linked to an event at most once.
/// </remarks>
public sealed class EventVehicle
{
    /// <summary>
    /// Gets or sets the identifier of the event the vehicle appears in.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the vehicle that appears in the event.
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Gets or sets the event navigation.
    /// </summary>
    public SourceMaterialEvent SourceMaterialEvent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the vehicle navigation.
    /// </summary>
    public Vehicle Vehicle { get; set; } = null!;
}
