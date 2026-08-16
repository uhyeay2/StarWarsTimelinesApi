using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a single piece of Star Wars media (movie, book, comic, show, video game, or short film)
/// that users can track in their personal library.
/// </summary>
/// <remarks>
/// Instances form the shared catalog and are managed by administrators. They are never queried with their
/// associated <see cref="UserSourceMaterial"/> records; user tracking is only accessed per user through the
/// library repository. Lazy loading is not enabled, so no related data is fetched unless explicitly included.
/// </remarks>
public sealed class SourceMaterial
{
    /// <summary>
    /// Gets or sets the unique identifier of the source material.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display title of the source material (for example, "Star Wars: A New Hope").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the medium the source material was originally released in.
    /// </summary>
    public Medium Medium { get; set; }

    /// <summary>
    /// Gets or sets the continuity (canonical, Legends, or both) that the source material belongs to.
    /// </summary>
    public CanonType CanonType { get; set; }

    /// <summary>
    /// Gets or sets the sub-units (episodes, chapters, issues, or levels) the source material is divided into.
    /// </summary>
    /// <remarks>
    /// This collection is only populated by library queries that explicitly include it, so it is empty on plain
    /// catalog reads. Lazy loading is not enabled.
    /// </remarks>
    public ICollection<SourceMaterialUnit> SourceMaterialUnits { get; set; } = [];
}
