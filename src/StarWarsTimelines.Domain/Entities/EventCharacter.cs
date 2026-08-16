namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents the many-to-many link between a <see cref="SourceMaterialEvent"/> and a <see cref="Character"/>.
/// </summary>
/// <remarks>
/// The entity is identified by the composite key (<see cref="EventId"/>, <see cref="CharacterId"/>) so a character
/// can be linked to an event at most once.
/// </remarks>
public sealed class EventCharacter
{
    /// <summary>
    /// Gets or sets the identifier of the event the character appears in.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the character that appears in the event.
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// Gets or sets the event navigation.
    /// </summary>
    public SourceMaterialEvent SourceMaterialEvent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the character navigation.
    /// </summary>
    public Character Character { get; set; } = null!;
}
