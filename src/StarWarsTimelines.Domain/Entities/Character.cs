namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a named character from the Star Wars universe that can be linked to timeline events.
/// </summary>
/// <remarks>
/// Characters form an admin-managed lookup catalog. They are never queried with their associated events; the
/// linking is owned by the <see cref="EventCharacter"/> table, so this entity has no inverse collection.
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
}
