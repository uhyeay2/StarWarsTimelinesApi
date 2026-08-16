using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to partially update a timeline event and its links.
/// </summary>
/// <param name="Title">The new display title, or <c>null</c> to leave it unchanged.</param>
/// <param name="Description">The new summary, or <c>null</c> to leave it unchanged.</param>
/// <param name="CanonType">The new continuity, or <c>null</c> to leave it unchanged.</param>
/// <param name="Year">The new numeric year, or <c>null</c> to leave it unchanged.</param>
/// <param name="DisplayDate">The new formatted display date, or <c>null</c> to leave it unchanged.</param>
/// <param name="DisplayDateEnd">The new end display date, or <c>null</c> to leave it unchanged.</param>
/// <param name="SourceMaterialId">The new source material identifier, or <c>null</c> to leave it unchanged.</param>
/// <param name="SourceMaterialUnitId">The new sub-unit identifier, or <c>null</c> to leave it unchanged.</param>
/// <param name="CharacterIds">The new set of character links, or <c>null</c> to leave them unchanged.</param>
/// <param name="LocationIds">The new set of location links, or <c>null</c> to leave them unchanged.</param>
/// <param name="VehicleIds">The new set of vehicle links, or <c>null</c> to leave them unchanged.</param>
public record UpdateSourceMaterialEventRequest(
    string? Title,
    string? Description,
    CanonType? CanonType,
    int? Year,
    string? DisplayDate,
    string? DisplayDateEnd,
    Guid? SourceMaterialId,
    Guid? SourceMaterialUnitId,
    IReadOnlyList<Guid>? CharacterIds,
    IReadOnlyList<Guid>? LocationIds,
    IReadOnlyList<Guid>? VehicleIds);
