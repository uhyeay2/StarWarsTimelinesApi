using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages a user's own account settings.
/// </summary>
public sealed class AccountService : IAccountService
{
    private const int VerificationTokenBytes = 32;
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _users;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<object> _hasher = new PasswordHasher<object>();

    /// <summary>
    /// Creates a new instance of the <see cref="AccountService"/>.
    /// </summary>
    /// <param name="users">The repository used to look up and persist user accounts.</param>
    /// <param name="email">The sender used to deliver verification emails.</param>
    /// <param name="emailOptions">The configuration used to build verification links.</param>
    /// <param name="unitOfWork">The unit of work used to commit account changes.</param>
    public AccountService(
        IUserRepository users,
        IEmailSender email,
        EmailOptions emailOptions,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _email = email;
        _emailOptions = emailOptions;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<UserAccountResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : UserAccountResponse.FromEntity(user);
    }

    /// <inheritdoc />
    public async Task<UserAccountResponse?> UpdateDisplayNameAsync(
        Guid userId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var name = displayName.Trim();
        if (name.Length == 0)
        {
            throw new BadRequestException("A display name is required.", nameof(displayName));
        }

        user.DisplayName = name;
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UserAccountResponse.FromEntity(user);
    }

    /// <inheritdoc />
    public async Task<UserAccountResponse?> UpdateEmailAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (normalizedEmail.Length == 0 || !normalizedEmail.Contains('@'))
        {
            throw new BadRequestException("A valid email address is required.", nameof(email));
        }

        if (normalizedEmail != user.Email)
        {
            if (await _users.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
            {
                throw new EntityAlreadyExistsException(
                    "A user with this email address is already registered.",
                    nameof(email));
            }

            user.Email = normalizedEmail;
            user.EmailVerifiedAtUtc = null;
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpiresAtUtc = null;
            _users.Update(user);
            await SendVerificationEmailAsync(user, cancellationToken);
        }

        return UserAccountResponse.FromEntity(user);
    }

    /// <inheritdoc />
    public async Task<UserAccountResponse?> UpdatePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var verification = _hasher.VerifyHashedPassword(null!, user.PasswordHash, currentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new BadRequestException("The current password is incorrect.", nameof(currentPassword));
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            throw new BadRequestException("A new password is required.", nameof(newPassword));
        }

        if (newPassword.Length < 6)
        {
            throw new BadRequestException("The new password must be at least six characters long.", nameof(newPassword));
        }

        user.PasswordHash = _hasher.HashPassword(null!, newPassword);
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UserAccountResponse.FromEntity(user);
    }

    /// <summary>
    /// Generates a fresh verification token, persists it on the user, and emails the verification link.
    /// </summary>
    /// <param name="user">The account whose new email address needs verification.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private async Task SendVerificationEmailAsync(User user, CancellationToken cancellationToken)
    {
        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(VerificationTokenBytes));
        user.EmailVerificationTokenHash = HashToken(verificationToken);
        user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow + VerificationTokenLifetime;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var verificationUrl = $"{_emailOptions.VerificationUrl}?token={verificationToken}";
        await _email.SendAsync(
            user.Email,
            "Verify your Star Wars Timelines account",
            $"<p>Hello {user.DisplayName},</p>" +
            $"<p>You changed the email address on your Star Wars Timelines account to <strong>{user.Email}</strong>.</p>" +
            "<p>Please confirm this address by clicking the link below:</p>" +
            $"<p><a href=\"{verificationUrl}\">Verify my email</a></p>" +
            "<p>This link expires in 24 hours. If you did not make this change, you can ignore this message.</p>",
            cancellationToken);
    }

    /// <summary>
    /// Computes the SHA-256 hash of a verification token, hex-encoded for storage.
    /// </summary>
    /// <param name="token">The raw verification token.</param>
    /// <returns>The hex-encoded SHA-256 hash of the token.</returns>
    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
