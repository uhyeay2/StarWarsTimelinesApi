using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to partially update a source material unit. Omitted (null) fields are left unchanged.
/// </summary>
/// <param name="UnitType">The new kind of unit, or <c>null</c> to leave it unchanged.</param>
/// <param name="Number">The new position within the source material, or <c>null</c> to leave it unchanged.</param>
/// <param name="Title">The new display title, or <c>null</c> to leave it unchanged.</param>
public record UpdateSourceMaterialUnitRequest(UnitType? UnitType, int? Number, string? Title);
