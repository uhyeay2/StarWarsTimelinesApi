namespace StarWarsTimelines.Domain.Enums;

/// <summary>
/// Specifies a user's progress on a tracked source material in their library.
/// </summary>
public enum TrackingStatus
{
    /// <summary>The user has started but not finished the source material.</summary>
    InProgress,

    /// <summary>The user has finished the source material.</summary>
    Completed,

    /// <summary>The user has added the source material to their library but has not started it.</summary>
    WishListed
}
