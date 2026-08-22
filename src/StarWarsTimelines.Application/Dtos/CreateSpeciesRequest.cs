namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new species in the catalog.
/// </summary>
/// <param name="Name">The species' name.</param>
/// <param name="HomePlanetId">
/// The identifier of the planet the species originates from, or <c>null</c> when the home planet is unknown.
/// </param>
public record CreateSpeciesRequest(string Name, Guid? HomePlanetId = null);
