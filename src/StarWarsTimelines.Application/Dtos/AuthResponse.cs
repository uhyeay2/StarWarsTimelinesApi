namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the result of a successful authentication.
/// </summary>
/// <param name="Token">The signed JWT bearer token to use for subsequent requests.</param>
/// <param name="User">The authenticated user.</param>
public record AuthResponse(string Token, UserResponse User);
