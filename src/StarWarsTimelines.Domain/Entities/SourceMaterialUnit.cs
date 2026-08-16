using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a single sub-unit of a <see cref="SourceMaterial"/> (for example, one episode of a show, one chapter
/// of a book, one issue of a comic, or one level of a video game).
/// </summary>
/// <remarks>
/// Units form part of the shared admin-managed catalog. A source material can have at most one unit per number
/// (enforced by a unique index), so the ordering of a unit within its material is unambiguous. Lazy loading is not
/// enabled, so no related data is fetched unless explicitly included.
/// </remarks>
public sealed class SourceMaterialUnit
{
    /// <summary>
    /// Gets or sets the unique identifier of the unit.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the <see cref="SourceMaterial"/> the unit belongs to.
    /// </summary>
    public Guid SourceMaterialId { get; set; }

    /// <summary>
    /// Gets or sets the kind of unit (episode, chapter, issue, or level).
    /// </summary>
    public UnitType UnitType { get; set; }

    /// <summary>
    /// Gets or sets the unit's position within its source material, starting at 1.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets an optional display title for the unit, or <c>null</c> when the unit has no individual title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the unit was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the owning <see cref="SourceMaterial"/> navigation.
    /// </summary>
    public SourceMaterial SourceMaterial { get; set; } = null!;
}
