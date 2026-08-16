namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the result of creating a new user account.
/// </summary>
/// <param name="UserId">The unique identifier of the created account.</param>
/// <param name="Username">The user's login name.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Email">The user's normalized email address.</param>
public record RegisterResponse(Guid UserId, string Username, string DisplayName, string Email);
