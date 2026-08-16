using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// The reason a login attempt failed.
/// </summary>
public enum LoginFailure
{
    /// <summary>
    /// The username or password did not match a known account.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// The account exists and the credentials are correct, but the email address has not been verified yet.
    /// </summary>
    EmailNotVerified
}

/// <summary>
/// The outcome of a login attempt.
/// </summary>
/// <param name="Auth">The authentication payload, or <c>null</c> when the attempt failed.</param>
/// <param name="Failure">The reason the attempt failed, or <c>null</c> on success.</param>
public sealed record AuthenticateResult(AuthResponse? Auth, LoginFailure? Failure)
{
    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    /// <param name="auth">The authentication payload to return.</param>
    /// <returns>A result carrying the authentication payload.</returns>
    public static AuthenticateResult Succeeded(AuthResponse auth) => new(auth, null);

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    /// <param name="failure">The reason the attempt failed.</param>
    /// <returns>A result carrying the failure reason.</returns>
    public static AuthenticateResult Failed(LoginFailure failure) => new(null, failure);
}
