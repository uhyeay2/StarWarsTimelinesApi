using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a vehicle as returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the vehicle.</param>
/// <param name="Name">The vehicle's name.</param>
public record VehicleResponse(Guid Id, string Name)
{
    /// <summary>
    /// Maps a <see cref="Vehicle"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The vehicle entity to map.</param>
    /// <returns>A <see cref="VehicleResponse"/> populated from the entity.</returns>
    public static VehicleResponse FromEntity(Vehicle item) => new(item.Id, item.Name);
}
