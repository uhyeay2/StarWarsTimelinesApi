using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages a user's own account settings.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Gets a user's account details, or <c>null</c> when no such account exists.
    /// </summary>
    /// <param name="userId">The identifier of the account to read.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The account, or <c>null</c> when the user does not exist.</returns>
    Task<UserAccountResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's display name, or returns <c>null</c> when no such account exists.
    /// </summary>
    /// <param name="userId">The identifier of the account to update.</param>
    /// <param name="displayName">The new display name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated account, or <c>null</c> when the user does not exist.</returns>
    /// <exception cref="BadRequestException">Thrown when the display name is blank.</exception>
    Task<UserAccountResponse?> UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's email address, marks the account as unverified, and emails a fresh verification link to the
    /// new address, or returns <c>null</c> when no such account exists.
    /// </summary>
    /// <param name="userId">The identifier of the account to update.</param>
    /// <param name="email">The new email address.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated account, or <c>null</c> when the user does not exist.</returns>
    /// <exception cref="BadRequestException">Thrown when the email is invalid.</exception>
    /// <exception cref="EntityAlreadyExistsException">Thrown when the email is already in use by another account.</exception>
    Task<UserAccountResponse?> UpdateEmailAsync(Guid userId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password after verifying their current password, or returns <c>null</c> when no such account
    /// exists.
    /// </summary>
    /// <param name="userId">The identifier of the account to update.</param>
    /// <param name="currentPassword">The user's current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated account, or <c>null</c> when the user does not exist.</returns>
    /// <exception cref="BadRequestException">
    /// Thrown when the current password is incorrect or the new password is invalid.
    /// </exception>
    Task<UserAccountResponse?> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
