namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to exchange a refresh token for a new token pair.
/// </summary>
/// <param name="RefreshToken">The opaque refresh token string issued during login or a previous refresh.</param>
public record RefreshTokenRequest(string RefreshToken);
