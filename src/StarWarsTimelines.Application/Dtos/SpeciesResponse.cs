using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a species as returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the species.</param>
/// <param name="Name">The species' name.</param>
/// <param name="HomePlanetId">
/// The unique identifier of the planet the species originates from, or <c>null</c> when it is unknown.
/// </param>
/// <param name="HomePlanetName">
/// The name of the planet the species originates from, or <c>null</c> when it is unknown.
/// </param>
public record SpeciesResponse(Guid Id, string Name, Guid? HomePlanetId, string? HomePlanetName)
{
    /// <summary>
    /// Maps a <see cref="Species"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The species entity to map.</param>
    /// <returns>A <see cref="SpeciesResponse"/> populated from the entity.</returns>
    public static SpeciesResponse FromEntity(Species item) =>
        new(item.Id, item.Name, item.HomePlanetId, item.HomePlanet?.Name);
}
