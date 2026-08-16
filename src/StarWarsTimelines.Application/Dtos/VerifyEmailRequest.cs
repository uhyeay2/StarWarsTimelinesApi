namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to verify a user's email address.
/// </summary>
/// <param name="Token">The verification token issued when the account was registered.</param>
public record VerifyEmailRequest(string Token);
