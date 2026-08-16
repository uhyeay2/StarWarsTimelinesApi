using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to partially update an existing source material. Omitted (null) fields are left unchanged.
/// </summary>
/// <param name="Title">The new display title, or <c>null</c> to leave it unchanged.</param>
/// <param name="Medium">The new medium, or <c>null</c> to leave it unchanged.</param>
/// <param name="CanonType">The new continuity, or <c>null</c> to leave it unchanged.</param>
public record UpdateSourceMaterialRequest(string? Title, Medium? Medium, CanonType? CanonType);
