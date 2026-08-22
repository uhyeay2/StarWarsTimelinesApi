namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to update a species. The request replaces the species' data: fields sent as
/// <c>null</c> are cleared, and required fields must be supplied on every call.
/// </summary>
/// <param name="Name">The new name. Required; a blank value is rejected.</param>
/// <param name="HomePlanetId">The home planet identifier, or <c>null</c> for an unknown home planet.</param>
public record UpdateSpeciesRequest(string Name, Guid? HomePlanetId);
