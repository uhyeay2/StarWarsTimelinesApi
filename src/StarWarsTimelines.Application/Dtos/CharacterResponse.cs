using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a character as returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the character.</param>
/// <param name="Name">The character's name.</param>
public record CharacterResponse(Guid Id, string Name)
{
    /// <summary>
    /// Maps a <see cref="Character"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The character entity to map.</param>
    /// <returns>A <see cref="CharacterResponse"/> populated from the entity.</returns>
    public static CharacterResponse FromEntity(Character item) => new(item.Id, item.Name);
}
