namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a user's progress on a single <see cref="SourceMaterialUnit"/> of a source material they track.
/// </summary>
/// <remarks>
/// The entity is identified by the composite key (<see cref="UserId"/>, <see cref="SourceMaterialUnitId"/>) so a
/// user can record progress for each unit at most once. A row exists only once the user has explicitly marked the
/// unit as completed or in progress, so absence means "not started".
/// </remarks>
public sealed class UserSourceMaterialUnit
{
    /// <summary>
    /// Gets or sets the identifier of the <see cref="User"/> who owns this progress record.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the <see cref="SourceMaterialUnit"/> being tracked.
    /// </summary>
    public Guid SourceMaterialUnitId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has completed the unit.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the progress record was last changed.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the owning <see cref="User"/> navigation.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the tracked <see cref="SourceMaterialUnit"/> navigation.
    /// </summary>
    public SourceMaterialUnit SourceMaterialUnit { get; set; } = null!;
}
