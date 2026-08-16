using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a user's personal tracking record for a single <see cref="SourceMaterial"/> in their library.
/// </summary>
/// <remarks>
/// The entity is identified by the composite key (<see cref="UserId"/>, <see cref="SourceMaterialId"/>) so a user
/// can track each source material at most once. It links a <see cref="User"/> to a <see cref="SourceMaterial"/>
/// and carries the per-user tracking state (status and favorite).
/// </remarks>
public sealed class UserSourceMaterial
{
    /// <summary>
    /// Gets or sets the identifier of the <see cref="User"/> who owns this library item.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the <see cref="SourceMaterial"/> being tracked.
    /// </summary>
    public Guid SourceMaterialId { get; set; }

    /// <summary>
    /// Gets or sets the user's progress status for this source material.
    /// </summary>
    public TrackingStatus Status { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has marked this source material as a favorite.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Gets or sets the user's position for this item within their library. Items are ordered by this value and then
    /// by <see cref="CreatedAtUtc"/>; users may reorder their library through the reorder endpoint.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the library item was first added.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the library item was last updated, or <c>null</c> if it was never updated.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the owning <see cref="User"/> navigation.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the tracked <see cref="SourceMaterial"/> navigation.
    /// </summary>
    /// <remarks>
    /// This is the only related data loaded with library queries (via an explicit include), together with the
    /// material's <see cref="SourceMaterial.SourceMaterialUnits"/>, because both are required to render a library
    /// item with its sub-unit progress. Lazy loading is not enabled.
    /// </remarks>
    public SourceMaterial SourceMaterial { get; set; } = null!;
}
