namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the result of a successful authentication.
/// </summary>
/// <param name="AccessToken">The signed JWT bearer token to use for subsequent requests.</param>
/// <param name="RefreshToken">The opaque refresh token that can be exchanged for a new token pair.</param>
/// <param name="User">The authenticated user.</param>
public record AuthResponse(string AccessToken, string RefreshToken, UserResponse User);
