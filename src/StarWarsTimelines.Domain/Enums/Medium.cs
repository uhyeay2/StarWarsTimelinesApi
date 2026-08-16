namespace StarWarsTimelines.Domain.Enums;

/// <summary>
/// Specifies the format in which a source material was originally released.
/// </summary>
public enum Medium
{
    /// <summary>A theatrical or streaming film.</summary>
    Movie,

    /// <summary>A novel or written book.</summary>
    Book,

    /// <summary>A comic book or graphic novel.</summary>
    Comic,

    /// <summary>An animated television series.</summary>
    AnimatedShow,

    /// <summary>A live-action television series.</summary>
    LiveActionShow,

    /// <summary>A video game.</summary>
    VideoGame,

    /// <summary>A short film.</summary>
    ShortFilm
}
