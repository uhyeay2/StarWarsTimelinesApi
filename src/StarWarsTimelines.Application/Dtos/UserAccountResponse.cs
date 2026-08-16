using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a user account as returned by the account settings endpoints.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Username">The user's login name.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Email">The user's normalized email address.</param>
/// <param name="EmailVerified">Whether the account's email address has been verified.</param>
/// <param name="Role">The user's authorization role.</param>
public record UserAccountResponse(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    bool EmailVerified,
    UserRole Role)
{
    /// <summary>
    /// Maps a <see cref="User"/> entity to an account response DTO.
    /// </summary>
    /// <param name="user">The user entity to map.</param>
    /// <returns>A <see cref="UserAccountResponse"/> populated from the entity.</returns>
    public static UserAccountResponse FromEntity(User user) =>
        new(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Email,
            user.EmailVerifiedAtUtc is not null,
            user.Role);
}
