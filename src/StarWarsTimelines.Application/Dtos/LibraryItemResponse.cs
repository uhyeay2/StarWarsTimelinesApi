using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a single library item (a source material tracked by a user) as returned by the API, including the
/// user's progress on each of the material's sub-units.
/// </summary>
/// <param name="SourceMaterialId">The identifier of the tracked source material.</param>
/// <param name="Title">The display title of the tracked source material.</param>
/// <param name="Medium">The medium of the tracked source material.</param>
/// <param name="CanonType">The continuity of the tracked source material.</param>
/// <param name="Status">
/// The user's progress status for this source material. When the material has sub-units this is derived from unit
/// progress; otherwise it is the manually tracked status.
/// </param>
/// <param name="IsFavorite">A value indicating whether the user has marked this item as a favorite.</param>
/// <param name="Units">The source material's sub-units with the user's per-unit progress, ordered by number.</param>
public record LibraryItemResponse(
    Guid SourceMaterialId,
    string Title,
    Medium Medium,
    CanonType CanonType,
    TrackingStatus Status,
    bool IsFavorite,
    IReadOnlyList<LibraryUnitResponse> Units)
{
    /// <summary>
    /// Maps a <see cref="UserSourceMaterial"/> entity and the user's unit progress to a response DTO.
    /// </summary>
    /// <param name="item">The library entity to map. Its <see cref="UserSourceMaterial.SourceMaterial"/> navigation must be loaded.</param>
    /// <param name="status">The effective status to report, which may differ from the stored status when the material has sub-units.</param>
    /// <param name="units">The material's units together with the user's progress, ordered by number.</param>
    /// <returns>A <see cref="LibraryItemResponse"/> populated from the entity.</returns>
    public static LibraryItemResponse FromEntity(UserSourceMaterial item, TrackingStatus status, IReadOnlyList<LibraryUnitResponse> units) =>
        new(
            item.SourceMaterialId,
            item.SourceMaterial.Title,
            item.SourceMaterial.Medium,
            item.SourceMaterial.CanonType,
            status,
            item.IsFavorite,
            units);
}
