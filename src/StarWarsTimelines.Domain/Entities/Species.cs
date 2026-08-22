namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a named species from the Star Wars universe that characters can belong to.
/// </summary>
/// <remarks>
/// Species form an admin-managed lookup catalog. Characters reference their species through an optional foreign key,
/// so this entity has no inverse collection back to characters.
/// </remarks>
public sealed class Species
{
    /// <summary>
    /// Gets or sets the unique identifier of the species.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the species' name (for example, "Twi'lek").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the <see cref="Location"/> the species originates from, or <c>null</c> when
    /// the home planet is unknown.
    /// </summary>
    public Guid? HomePlanetId { get; set; }

    /// <summary>
    /// Gets or sets the planet the species originates from, or <c>null</c> when the home planet is unknown.
    /// </summary>
    public Location? HomePlanet { get; set; }
}
