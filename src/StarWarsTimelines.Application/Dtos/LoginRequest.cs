namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to authenticate a user.
/// </summary>
/// <param name="Username">The user's login name.</param>
/// <param name="Password">The user's plain-text password.</param>
public record LoginRequest(string Username, string Password);
