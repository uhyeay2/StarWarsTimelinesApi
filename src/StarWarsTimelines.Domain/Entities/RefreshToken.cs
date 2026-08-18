namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Stores a refresh token that can be exchanged for a new access/refresh token pair.
/// Refresh tokens are single-use: every exchange rotates the old token and issues a new one.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The user this refresh token belongs to.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The opaque token string sent to the client.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// The UTC date/time the token expires and can no longer be exchanged.
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>
    /// The UTC date/time the token was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// The UTC date/time the token was revoked, or <c>null</c> if it is still active.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// When this token is replaced by a new one during rotation, this stores the new token's identifier
    /// so that any subsequent use of the old token can be treated as a potential token-reuse attack.
    /// </summary>
    public string? ReplacedByToken { get; set; }
}
