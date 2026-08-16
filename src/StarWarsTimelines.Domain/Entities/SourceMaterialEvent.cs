using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a single entry on the Star Wars timeline, describing an event drawn from a source material and
/// linking the characters, locations, and vehicles involved in it.
/// </summary>
/// <remarks>
/// Events are authored against the <see cref="SourceMaterial"/> catalog and reference it through
/// <see cref="SourceMaterialId"/>. When an event depicts a specific sub-unit of the source material — one episode
/// of a show, one chapter of a book, one issue of a comic, or one level of a game — it can reference that unit
/// through <see cref="SourceMaterialUnitId"/>. The event owns its many-to-many links (<see cref="EventCharacters"/>,
/// <see cref="EventLocations"/>, <see cref="EventVehicles"/>); the linked catalog entities have no inverse
/// collections.
/// </remarks>
public sealed class SourceMaterialEvent
{
    /// <summary>
    /// Gets or sets the unique identifier of the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display title of the event (for example, "The Battle of Yavin").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable summary of what happened during the event.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the continuity the event belongs to.
    /// </summary>
    public CanonType CanonType { get; set; }

    /// <summary>
    /// Gets or sets the numeric year of the event on the galactic timeline (negative for BBY, positive for ABY).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the formatted display date of the event (for example, "0 BBY").
    /// </summary>
    public string DisplayDate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the formatted display date marking the end of the event's span, or <c>null</c> when the event
    /// occurred at a single point in time.
    /// </summary>
    public string? DisplayDateEnd { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the <see cref="SourceMaterial"/> the event is drawn from.
    /// </summary>
    public Guid SourceMaterialId { get; set; }

    /// <summary>
    /// Gets or sets the source material the event is drawn from.
    /// </summary>
    public SourceMaterial SourceMaterial { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the <see cref="SourceMaterialUnit"/> the event depicts, or <c>null</c> when the
    /// event covers the whole source material rather than a single sub-unit.
    /// </summary>
    public Guid? SourceMaterialUnitId { get; set; }

    /// <summary>
    /// Gets or sets the specific sub-unit of the source material the event depicts, or <c>null</c> when the event
    /// covers the whole source material.
    /// </summary>
    public SourceMaterialUnit? SourceMaterialUnit { get; set; }

    /// <summary>
    /// Gets or sets the characters that appear in the event.
    /// </summary>
    public ICollection<EventCharacter> EventCharacters { get; set; } = [];

    /// <summary>
    /// Gets or sets the locations the event takes place in.
    /// </summary>
    public ICollection<EventLocation> EventLocations { get; set; } = [];

    /// <summary>
    /// Gets or sets the vehicles that appear in the event.
    /// </summary>
    public ICollection<EventVehicle> EventVehicles { get; set; } = [];
}
