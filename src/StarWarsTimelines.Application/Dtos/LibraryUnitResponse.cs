using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a single source material unit together with the requesting user's progress on it.
/// </summary>
/// <param name="Id">The unique identifier of the unit.</param>
/// <param name="UnitType">The kind of unit (episode, chapter, issue, or level).</param>
/// <param name="GroupNumber">The group the unit belongs to (season for a show, volume for a comic), or <c>null</c>.</param>
/// <param name="Number">The unit's position within its group or source material.</param>
/// <param name="Title">The optional display title of the unit.</param>
/// <param name="IsCompleted">A value indicating whether the user has completed the unit.</param>
/// <param name="IsTracked">A value indicating whether the user has any explicit progress record for the unit;
/// library responses include every unit of the material, so this distinguishes units the user has actually
/// tracked from untouched ones.</param>
public record LibraryUnitResponse(Guid Id, UnitType UnitType, int? GroupNumber, int Number, string? Title, bool IsCompleted, bool IsTracked = false)
{
    /// <summary>
    /// Maps a <see cref="SourceMaterialUnit"/> entity and the user's progress flags to a response DTO.
    /// </summary>
    /// <param name="item">The unit entity to map.</param>
    /// <param name="isCompleted">The requesting user's completion state for the unit.</param>
    /// <param name="isTracked">Whether the user has an explicit progress record for the unit.</param>
    /// <returns>A <see cref="LibraryUnitResponse"/> populated from the entity.</returns>
    public static LibraryUnitResponse FromEntity(SourceMaterialUnit item, bool isCompleted, bool isTracked = false) =>
        new(item.Id, item.UnitType, item.GroupNumber, item.Number, item.Title, isCompleted, isTracked);
}
