using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new sub-unit for a source material.
/// </summary>
/// <param name="UnitType">The kind of unit (episode, chapter, issue, or level).</param>
/// <param name="GroupNumber">The group the unit belongs to (season for a show, volume for a comic), or <c>null</c> for
/// materials without groups.</param>
/// <param name="Number">The unit's position within its group (or source material when it has no group), starting at 1.</param>
/// <param name="Title">An optional display title for the unit.</param>
public record CreateSourceMaterialUnitRequest(UnitType UnitType, int? GroupNumber, int Number, string? Title);
