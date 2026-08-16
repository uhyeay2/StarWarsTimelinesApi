namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to update a user's email address.
/// </summary>
/// <param name="Email">The user's new email address. The address must be verified again before the account can log in.</param>
public record UpdateEmailRequest(string Email);
