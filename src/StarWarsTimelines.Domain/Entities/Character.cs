namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a named character from the Star Wars universe that can be linked to timeline events.
/// </summary>
/// <remarks>
/// Characters form an admin-managed lookup catalog. They are never queried with their associated events; the
/// linking is owned by the <see cref="EventCharacter"/> table, so this entity has no inverse collection for events.
/// Biographical attributes (birth planet, birth and death years, and species) are optional because they are
/// unknown for many characters.
/// </remarks>
public sealed class Character
{
    /// <summary>
    /// Gets or sets the unique identifier of the character.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the character's name (for example, "Luke Skywalker").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the <see cref="Location"/> the character was born on, or <c>null</c> when
    /// the character's birth planet is unknown.
    /// </summary>
    public Guid? PlanetBornOnId { get; set; }

    /// <summary>
    /// Gets or sets the planet the character was born on, or <c>null</c> when it is unknown.
    /// </summary>
    public Location? PlanetBornOn { get; set; }

    /// <summary>
    /// Gets or sets the chronologically earliest year the character could have been born in on the galactic
    /// timeline (negative for BBY, positive for ABY), or <c>null</c> when the birth year is unknown. An exact
    /// birth year sets this equal to <see cref="YearOfBirthLatest"/>.
    /// </summary>
    public int? YearOfBirthEarliest { get; set; }

    /// <summary>
    /// Gets or sets the chronologically latest year the character could have been born in on the galactic
    /// timeline (negative for BBY, positive for ABY), or <c>null</c> when the birth year is unknown. An exact
    /// birth year sets this equal to <see cref="YearOfBirthEarliest"/>.
    /// </summary>
    public int? YearOfBirthLatest { get; set; }

    /// <summary>
    /// Gets or sets the chronologically earliest year the character could have died in on the galactic timeline
    /// (negative for BBY, positive for ABY), or <c>null</c> when the death year is unknown. An exact death year
    /// sets this equal to <see cref="YearOfDeathLatest"/>.
    /// </summary>
    public int? YearOfDeathEarliest { get; set; }

    /// <summary>
    /// Gets or sets the chronologically latest year the character could have died in on the galactic timeline
    /// (negative for BBY, positive for ABY), or <c>null</c> when the death year is unknown. An exact death year
    /// sets this equal to <see cref="YearOfDeathEarliest"/>.
    /// </summary>
    public int? YearOfDeathLatest { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the <see cref="Species"/> the character belongs to, or <c>null</c> when the
    /// character's species is unknown.
    /// </summary>
    public Guid? SpeciesId { get; set; }

    /// <summary>
    /// Gets or sets the species the character belongs to, or <c>null</c> when it is unknown.
    /// </summary>
    public Species? Species { get; set; }
}
