using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="User"/> accounts.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="User"/>, or <c>null</c>.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their unique login name, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="username">The user's login name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="User"/>, or <c>null</c>.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their normalized email address, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="email">The user's normalized email address (trimmed and lower-cased).</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="User"/>, or <c>null</c>.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by the SHA-256 hash of their pending email verification token, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash of the verification token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="User"/>, or <c>null</c>.</returns>
    Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new user for insertion.
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing user for update.
    /// </summary>
    /// <param name="user">The user to update.</param>
    void Update(User user);
}
