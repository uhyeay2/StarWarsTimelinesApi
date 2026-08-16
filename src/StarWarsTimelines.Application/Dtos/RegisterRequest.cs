namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new user account.
/// </summary>
/// <param name="Username">The user's unique login name.</param>
/// <param name="DisplayName">The optional human-readable name shown in the user interface. Defaults to the username
/// when not supplied.</param>
/// <param name="Email">The user's email address, used for verification and password recovery.</param>
/// <param name="Password">The user's plain-text password, which must be at least six characters long.</param>
public record RegisterRequest(string Username, string? DisplayName, string Email, string Password);
