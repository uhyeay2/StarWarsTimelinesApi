using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a timeline event as returned by the API, including its linked source material, characters,
/// locations, and vehicles.
/// </summary>
/// <param name="Id">The unique identifier of the event.</param>
/// <param name="Title">The display title of the event.</param>
/// <param name="Description">A human-readable summary of what happened during the event.</param>
/// <param name="CanonType">The continuity the event belongs to.</param>
/// <param name="Year">The numeric year of the event on the galactic timeline.</param>
/// <param name="DisplayDate">The formatted display date of the event.</param>
/// <param name="DisplayDateEnd">The formatted display date marking the end of the event's span, or <c>null</c>.</param>
/// <param name="SourceMaterial">The source material the event is drawn from.</param>
/// <param name="Characters">The characters that appear in the event.</param>
/// <param name="Locations">The locations the event takes place in.</param>
/// <param name="Vehicles">The vehicles that appear in the event.</param>
public record SourceMaterialEventResponse(
    Guid Id,
    string Title,
    string Description,
    CanonType CanonType,
    int Year,
    string DisplayDate,
    string? DisplayDateEnd,
    SourceMaterialResponse SourceMaterial,
    IReadOnlyList<CharacterResponse> Characters,
    IReadOnlyList<LocationResponse> Locations,
    IReadOnlyList<VehicleResponse> Vehicles)
{
    /// <summary>
    /// Maps a <see cref="SourceMaterialEvent"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The event entity to map.</param>
    /// <returns>A <see cref="SourceMaterialEventResponse"/> populated from the entity.</returns>
    public static SourceMaterialEventResponse FromEntity(SourceMaterialEvent item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.CanonType,
        item.Year,
        item.DisplayDate,
        item.DisplayDateEnd,
        SourceMaterialResponse.FromEntity(item.SourceMaterial),
        item.EventCharacters.Select(x => CharacterResponse.FromEntity(x.Character)).OrderBy(x => x.Name).ToList(),
        item.EventLocations.Select(x => LocationResponse.FromEntity(x.Location)).OrderBy(x => x.Name).ToList(),
        item.EventVehicles.Select(x => VehicleResponse.FromEntity(x.Vehicle)).OrderBy(x => x.Name).ToList());
}
