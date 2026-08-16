using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a source material as returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the source material.</param>
/// <param name="Title">The display title of the source material.</param>
/// <param name="Medium">The medium of the source material.</param>
/// <param name="CanonType">The continuity of the source material.</param>
public record SourceMaterialResponse(Guid Id, string Title, Medium Medium, CanonType CanonType)
{
    /// <summary>
    /// Maps a <see cref="SourceMaterial"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The source material entity to map.</param>
    /// <returns>A <see cref="SourceMaterialResponse"/> populated from the entity.</returns>
    public static SourceMaterialResponse FromEntity(SourceMaterial item) =>
        new(item.Id, item.Title, item.Medium, item.CanonType);
}
