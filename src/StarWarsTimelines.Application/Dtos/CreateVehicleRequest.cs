namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new vehicle in the catalog.
/// </summary>
/// <param name="Name">The vehicle's name.</param>
public record CreateVehicleRequest(string Name);
