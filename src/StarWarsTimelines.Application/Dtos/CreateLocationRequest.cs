namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new location in the catalog.
/// </summary>
/// <param name="Name">The location's name.</param>
public record CreateLocationRequest(string Name);
