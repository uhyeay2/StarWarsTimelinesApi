namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new character in the catalog.
/// </summary>
/// <param name="Name">The character's name.</param>
public record CreateCharacterRequest(string Name);
