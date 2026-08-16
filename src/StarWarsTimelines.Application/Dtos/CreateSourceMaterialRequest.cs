using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new source material in the catalog.
/// </summary>
/// <param name="Title">The display title of the source material.</param>
/// <param name="Medium">The medium of the source material; defaults to <see cref="Medium.Movie"/> when omitted.</param>
/// <param name="CanonType">The continuity of the source material; defaults to <see cref="CanonType.Canon"/> when omitted.</param>
public record CreateSourceMaterialRequest(string Title, Medium? Medium, CanonType? CanonType);
