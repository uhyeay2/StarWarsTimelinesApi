using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that registers users, authenticates them, and issues bearer tokens.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const int VerificationTokenBytes = 32;
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<object> _hasher = new PasswordHasher<object>();

    /// <summary>
    /// Creates a new instance of the <see cref="AuthService"/>.
    /// </summary>
    /// <param name="users">The repository used to look up and persist user accounts.</param>
    /// <param name="refreshTokens">The repository used to persist refresh tokens.</param>
    /// <param name="tokens">The service used to generate bearer tokens.</param>
    /// <param name="email">The sender used to deliver verification emails.</param>
    /// <param name="emailOptions">The configuration used to build verification links.</param>
    /// <param name="jwtOptions">The JWT configuration used to determine refresh token expiry.</param>
    /// <param name="unitOfWork">The unit of work used to commit account changes.</param>
    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokens,
        IEmailSender email,
        EmailOptions emailOptions,
        JwtOptions jwtOptions,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _email = email;
        _emailOptions = emailOptions;
        _jwtOptions = jwtOptions;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<AuthenticateResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return AuthenticateResult.Failed(LoginFailure.InvalidCredentials);
        }

        var verification = _hasher.VerifyHashedPassword(null!, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return AuthenticateResult.Failed(LoginFailure.InvalidCredentials);
        }

        if (user.EmailVerifiedAtUtc is null)
        {
            return AuthenticateResult.Failed(LoginFailure.EmailNotVerified);
        }

        var token = _tokens.GenerateToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user, cancellationToken);
        return AuthenticateResult.Succeeded(new AuthResponse(token, refreshToken, UserResponse.FromEntity(user)));
    }

    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (username.Length == 0)
        {
            throw new BadRequestException("A username is required.", nameof(request.Username));
        }

        if (email.Length == 0 || !email.Contains('@'))
        {
            throw new BadRequestException("A valid email address is required.", nameof(request.Email));
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            throw new BadRequestException("A password is required.", nameof(request.Password));
        }

        if (request.Password.Length < 6)
        {
            throw new BadRequestException("The password must be at least six characters long.", nameof(request.Password));
        }

        if (await _users.GetByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new EntityAlreadyExistsException("A user with this username already exists.", nameof(request.Username));
        }

        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new EntityAlreadyExistsException("A user with this email address is already registered.", nameof(request.Email));
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? username : request.DisplayName.Trim(),
            Email = email,
            EmailVerifiedAtUtc = null,
            PasswordHash = _hasher.HashPassword(null!, request.Password),
            Role = UserRole.Standard,
            CreatedAtUtc = now
        };

        await _users.AddAsync(user, cancellationToken);
        await SendVerificationEmailAsync(user, cancellationToken);

        return new RegisterResponse(user.Id, user.Username, user.DisplayName, user.Email);
    }

    /// <inheritdoc />
    public async Task VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new BadRequestException("The verification token is missing.", nameof(token));
        }

        var user = await _users.GetByEmailVerificationTokenHashAsync(HashToken(token), cancellationToken);
        if (user is null || user.EmailVerificationTokenExpiresAtUtc is null ||
            user.EmailVerificationTokenExpiresAtUtc.Value < DateTime.UtcNow)
        {
            throw new InvalidTokenException("The verification link is invalid or has expired.", nameof(token));
        }

        if (user.EmailVerifiedAtUtc is not null)
        {
            return;
        }

        user.EmailVerifiedAtUtc = DateTime.UtcNow;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAtUtc = null;
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResendVerificationEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var identifier = usernameOrEmail.Trim();
        if (identifier.Length == 0)
        {
            throw new BadRequestException("A username or email address is required.", nameof(usernameOrEmail));
        }

        var user = await _users.GetByUsernameAsync(identifier, cancellationToken);
        if (user is null && identifier.Contains('@'))
        {
            user = await _users.GetByEmailAsync(identifier.ToLowerInvariant(), cancellationToken);
        }

        if (user is null || user.EmailVerifiedAtUtc is not null)
        {
            return;
        }

        _users.Update(user);
        await SendVerificationEmailAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new BadRequestException("A refresh token is required.", nameof(refreshToken));
        }

        var stored = await _refreshTokens.GetByTokenAsync(refreshToken, cancellationToken);
        if (stored is null)
        {
            throw new InvalidTokenException("The refresh token is invalid.", nameof(refreshToken));
        }

        if (stored.RevokedAtUtc is not null)
        {
            // Potential token-reuse attack — revoke the entire family.
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidTokenException("The refresh token has already been revoked.", nameof(refreshToken));
        }

        if (stored.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new InvalidTokenException("The refresh token has expired.", nameof(refreshToken));
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidTokenException("The user associated with this token no longer exists.", nameof(refreshToken));
        }

        // Rotate: revoke old, issue new.
        var newRefreshTokenValue = await IssueRefreshTokenAsync(user, cancellationToken);
        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByToken = newRefreshTokenValue;
        _refreshTokens.Update(stored);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokens.GenerateToken(user);
        return new AuthResponse(accessToken, newRefreshTokenValue, UserResponse.FromEntity(user));
    }

    /// <summary>
    /// Generates a refresh token, persists it, and returns the opaque string.
    /// </summary>
    /// <param name="user">The user to issue the token for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The opaque refresh token string.</returns>
    private async Task<string> IssueRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var value = _tokens.GenerateRefreshToken();
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = value,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _refreshTokens.AddAsync(entity, cancellationToken);
        return value;
    }

    /// <summary>
    /// Generates a fresh verification token, persists it on the user, and emails the verification link.
    /// </summary>
    /// <param name="user">The unverified account to email.</param>
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
            $"<p>Welcome to Star Wars Timelines, {user.DisplayName}!</p>" +
            "<p>Please confirm your email address by clicking the link below:</p>" +
            $"<p><a href=\"{verificationUrl}\">Verify my email</a></p>" +
            "<p>This link expires in 24 hours. If you did not create this account, you can ignore this message.</p>",
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
