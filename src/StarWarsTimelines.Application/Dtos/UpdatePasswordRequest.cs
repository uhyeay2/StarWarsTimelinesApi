namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to change a user's password.
/// </summary>
/// <param name="CurrentPassword">The user's current password, used to confirm the request.</param>
/// <param name="NewPassword">The new password, which must be at least six characters long.</param>
public record UpdatePasswordRequest(string CurrentPassword, string NewPassword);
