using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new timeline event referencing existing catalog entries.
/// </summary>
/// <param name="Title">The display title of the event.</param>
/// <param name="Description">A human-readable summary of what happened during the event.</param>
/// <param name="CanonType">The continuity the event belongs to.</param>
/// <param name="Year">The numeric year of the event on the galactic timeline.</param>
/// <param name="DisplayDate">The formatted display date of the event.</param>
/// <param name="DisplayDateEnd">The formatted display date marking the end of the event's span, or <c>null</c>.</param>
/// <param name="SourceMaterialId">The identifier of the source material the event is drawn from.</param>
/// <param name="CharacterIds">The identifiers of the characters that appear in the event.</param>
/// <param name="LocationIds">The identifiers of the locations the event takes place in.</param>
/// <param name="VehicleIds">The identifiers of the vehicles that appear in the event.</param>
public record CreateSourceMaterialEventRequest(
    string Title,
    string Description,
    CanonType CanonType,
    int Year,
    string DisplayDate,
    string? DisplayDateEnd,
    Guid SourceMaterialId,
    IReadOnlyList<Guid> CharacterIds,
    IReadOnlyList<Guid> LocationIds,
    IReadOnlyList<Guid> VehicleIds);
