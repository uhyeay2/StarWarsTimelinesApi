namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to update a character's name.
/// </summary>
/// <param name="Name">The new name, or <c>null</c> to leave it unchanged.</param>
public record UpdateCharacterRequest(string? Name);
