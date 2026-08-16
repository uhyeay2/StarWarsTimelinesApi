using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a sub-unit of a source material (episode, chapter, issue, or level) as returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the unit.</param>
/// <param name="SourceMaterialId">The identifier of the source material the unit belongs to.</param>
/// <param name="UnitType">The kind of unit.</param>
/// <param name="Number">The unit's position within its source material.</param>
/// <param name="Title">The optional display title of the unit.</param>
public record SourceMaterialUnitResponse(
    Guid Id,
    Guid SourceMaterialId,
    UnitType UnitType,
    int Number,
    string? Title)
{
    /// <summary>
    /// Maps a <see cref="SourceMaterialUnit"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The unit entity to map.</param>
    /// <returns>A <see cref="SourceMaterialUnitResponse"/> populated from the entity.</returns>
    public static SourceMaterialUnitResponse FromEntity(SourceMaterialUnit item) =>
        new(item.Id, item.SourceMaterialId, item.UnitType, item.Number, item.Title);
}
