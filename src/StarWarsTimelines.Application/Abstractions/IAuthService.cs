using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that registers users, authenticates them, and issues bearer tokens.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates a user's credentials and, when valid, returns a signed bearer token with the user's claims.
    /// </summary>
    /// <param name="username">The user's login name.</param>
    /// <param name="password">The user's plain-text password.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An <see cref="AuthenticateResult"/> carrying the token and user on success, or the reason the
    /// attempt failed.</returns>
    Task<AuthenticateResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user account with an unverified email address and emails the user a verification link.
    /// </summary>
    /// <param name="request">The registration payload.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="RegisterResponse"/> describing the created account.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the username or email is already registered, or the payload is invalid.
    /// </exception>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a user's email address using the token issued at registration.
    /// </summary>
    /// <param name="token">The verification token emailed to the user.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the token is missing, unknown, or expired.
    /// </exception>
    Task VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a fresh verification token and emails it to an unverified account.
    /// </summary>
    /// <param name="usernameOrEmail">The user's login name or email address.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <remarks>
    /// Returns without sending when no matching account exists or the account is already verified.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the identifier is blank.</exception>
    Task ResendVerificationEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid refresh token for a new access/refresh token pair.
    /// The old refresh token is revoked and cannot be reused.
    /// </summary>
    /// <param name="refreshToken">The opaque refresh token string.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An <see cref="AuthResponse"/> carrying the new access token, refresh token, and user.</returns>
    /// <exception cref="ArgumentException">Thrown when the refresh token is missing, unknown, revoked, or expired.</exception>
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}
