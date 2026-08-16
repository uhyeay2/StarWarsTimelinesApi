using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Creates signed JWT bearer tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT for the given user, embedding their identifier, username, and role as claims.
    /// </summary>
    /// <param name="user">The authenticated user to create a token for.</param>
    /// <returns>A serialized JWT bearer token.</returns>
    string GenerateToken(User user);
}
