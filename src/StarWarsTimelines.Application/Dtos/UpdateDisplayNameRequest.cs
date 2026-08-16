namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to update a user's display name.
/// </summary>
/// <param name="DisplayName">The new display name shown in the user interface.</param>
public record UpdateDisplayNameRequest(string DisplayName);
