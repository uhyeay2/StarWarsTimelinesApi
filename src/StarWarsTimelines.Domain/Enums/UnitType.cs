namespace StarWarsTimelines.Domain.Enums;

/// <summary>
/// Specifies the kind of sub-unit a source material is divided into for granular progress tracking.
/// </summary>
public enum UnitType
{
    /// <summary>A single episode of a television series.</summary>
    Episode,

    /// <summary>A single chapter of a book.</summary>
    Chapter,

    /// <summary>A single issue of a comic series.</summary>
    Issue,

    /// <summary>A full season of a television series, containing multiple episodes.</summary>
    Season,

    /// <summary>A single level or mission of a video game.</summary>
    Level
}
