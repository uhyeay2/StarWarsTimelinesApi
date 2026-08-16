using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a user account as returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Username">The user's login name.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Role">The user's authorization role.</param>
public record UserResponse(Guid Id, string Username, string DisplayName, UserRole Role)
{
    /// <summary>
    /// Maps a <see cref="User"/> entity to a response DTO.
    /// </summary>
    /// <param name="user">The user entity to map.</param>
    /// <returns>A <see cref="UserResponse"/> populated from the entity.</returns>
    public static UserResponse FromEntity(User user) =>
        new(user.Id, user.Username, user.DisplayName, user.Role);
}
