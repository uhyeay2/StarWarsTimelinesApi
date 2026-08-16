using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new sub-unit for a source material.
/// </summary>
/// <param name="UnitType">The kind of unit (episode, chapter, issue, or level).</param>
/// <param name="Number">The unit's position within its source material, starting at 1.</param>
/// <param name="Title">An optional display title for the unit.</param>
public record CreateSourceMaterialUnitRequest(UnitType UnitType, int Number, string? Title);
