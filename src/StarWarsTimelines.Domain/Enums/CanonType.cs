namespace StarWarsTimelines.Domain.Enums;

/// <summary>
/// Specifies the Star Wars continuity that a source material belongs to.
/// </summary>
public enum CanonType
{
    /// <summary>A source material set in the current canon continuity.</summary>
    Canon,

    /// <summary>A source material set in the former (pre-2014) Expanded Universe continuity.</summary>
    Legends,

    /// <summary>A source material that is considered part of both the canon and Legends continuities.</summary>
    CanonAndLegends
}
