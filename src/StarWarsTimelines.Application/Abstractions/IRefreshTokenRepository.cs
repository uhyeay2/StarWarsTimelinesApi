using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="RefreshToken"/> records.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by its opaque token string, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="token">The opaque token string.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="RefreshToken"/>, or <c>null</c>.</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new refresh token for insertion.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing refresh token for update (e.g. revoking or recording replacement).
    /// </summary>
    /// <param name="refreshToken">The refresh token to update.</param>
    void Update(RefreshToken refreshToken);

    /// <summary>
    /// Revokes all active refresh tokens for the specified user.
    /// </summary>
    /// <param name="userId">The user whose tokens should be revoked.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
