namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to resend an account's email verification link.
/// </summary>
/// <param name="UsernameOrEmail">The user's login name or email address.</param>
public record ResendVerificationEmailRequest(string UsernameOrEmail);
