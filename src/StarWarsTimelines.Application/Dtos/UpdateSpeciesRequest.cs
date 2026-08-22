namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to partially update a species. Properties left <c>null</c> are unchanged.
/// </summary>
/// <param name="Name">The new name, or <c>null</c> to leave it unchanged.</param>
/// <param name="HomePlanetId">
/// The new home planet identifier, or <c>null</c> to leave it unchanged.
/// </param>
public record UpdateSpeciesRequest(string? Name = null, Guid? HomePlanetId = null);
