namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents the many-to-many link between a <see cref="SourceMaterialEvent"/> and a <see cref="Location"/>.
/// </summary>
/// <remarks>
/// The entity is identified by the composite key (<see cref="EventId"/>, <see cref="LocationId"/>) so a location
/// can be linked to an event at most once.
/// </remarks>
public sealed class EventLocation
{
    /// <summary>
    /// Gets or sets the identifier of the event the location appears in.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location that appears in the event.
    /// </summary>
    public Guid LocationId { get; set; }

    /// <summary>
    /// Gets or sets the event navigation.
    /// </summary>
    public SourceMaterialEvent SourceMaterialEvent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the location navigation.
    /// </summary>
    public Location Location { get; set; } = null!;
}
