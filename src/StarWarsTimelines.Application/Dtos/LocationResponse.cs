using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a location as returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the location.</param>
/// <param name="Name">The location's name.</param>
public record LocationResponse(Guid Id, string Name)
{
    /// <summary>
    /// Maps a <see cref="Location"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The location entity to map.</param>
    /// <returns>A <see cref="LocationResponse"/> populated from the entity.</returns>
    public static LocationResponse FromEntity(Location item) => new(item.Id, item.Name);
}
