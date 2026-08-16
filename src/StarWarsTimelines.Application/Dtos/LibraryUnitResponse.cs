using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a single source material unit together with the requesting user's progress on it.
/// </summary>
/// <param name="Id">The unique identifier of the unit.</param>
/// <param name="UnitType">The kind of unit (episode, chapter, issue, or level).</param>
/// <param name="Number">The unit's position within its source material.</param>
/// <param name="Title">The optional display title of the unit.</param>
/// <param name="IsCompleted">A value indicating whether the user has completed the unit.</param>
public record LibraryUnitResponse(Guid Id, UnitType UnitType, int Number, string? Title, bool IsCompleted)
{
    /// <summary>
    /// Maps a <see cref="SourceMaterialUnit"/> entity and the user's progress flag to a response DTO.
    /// </summary>
    /// <param name="item">The unit entity to map.</param>
    /// <param name="isCompleted">The requesting user's completion state for the unit.</param>
    /// <returns>A <see cref="LibraryUnitResponse"/> populated from the entity.</returns>
    public static LibraryUnitResponse FromEntity(SourceMaterialUnit item, bool isCompleted) =>
        new(item.Id, item.UnitType, item.Number, item.Title, isCompleted);
}
