using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Tests;

public sealed class AuthServiceTests
{
    private const string VerificationUrl = "https://localhost:4200/verify-email";
    private static readonly JwtOptions JwtOptions = new("Issuer", "Audience", "secret-key", 120, 7);

    private readonly Mock<IUserRepository> _users;
    private readonly Mock<IRefreshTokenRepository> _refreshTokens;
    private readonly Mock<ITokenService> _tokens;
    private readonly Mock<IEmailSender> _email;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly AuthService _service;
    private readonly PasswordHasher<object> _hasher = new();

    public AuthServiceTests()
    {
        _users = new Mock<IUserRepository>();
        _refreshTokens = new Mock<IRefreshTokenRepository>();
        _tokens = new Mock<ITokenService>();
        _email = new Mock<IEmailSender>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new AuthService(
            _users.Object,
            _refreshTokens.Object,
            _tokens.Object,
            _email.Object,
            new EmailOptions(
                "no-reply@starwarstimelines.dev",
                "Star Wars Timelines",
                string.Empty,
                587,
                string.Empty,
                string.Empty,
                true,
                VerificationUrl),
            JwtOptions,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        _users.Setup(x => x.GetByUsernameAsync("padme", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokens.Setup(x => x.GenerateToken(user)).Returns("token-123");
        _tokens.Setup(x => x.GenerateRefreshToken()).Returns("refresh-abc");

        var result = await _service.AuthenticateAsync("padme", "padme123");

        Assert.NotNull(result.Auth);
        Assert.Null(result.Failure);
        Assert.Equal("token-123", result.Auth.AccessToken);
        Assert.Equal("refresh-abc", result.Auth.RefreshToken);
        Assert.Equal("padme", result.Auth.User.Username);
        Assert.Equal("Padmé Amidala", result.Auth.User.DisplayName);
        Assert.Equal(UserRole.Standard, result.Auth.User.Role);
        Assert.Equal(user.Id, result.Auth.User.Id);
        _tokens.Verify(x => x.GenerateToken(user), Times.Once);
        _refreshTokens.Verify(x => x.AddAsync(
            It.Is<RefreshToken>(rt => rt.Token == "refresh-abc" && rt.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithUnknownUser_ReportsInvalidCredentials()
    {
        _users
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.AuthenticateAsync("nobody", "wrong");

        Assert.Null(result.Auth);
        Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
        _tokens.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ReportsInvalidCredentials()
    {
        var user = SeedUser("admin", "admin123", UserRole.Admin);
        _users.Setup(x => x.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.AuthenticateAsync("admin", "wrong-password");

        Assert.Null(result.Auth);
        Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
        _tokens.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WithUnverifiedEmail_ReportsEmailNotVerified()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: false);
        _users.Setup(x => x.GetByUsernameAsync("padme", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.AuthenticateAsync("padme", "padme123");

        Assert.Null(result.Auth);
        Assert.Equal(LoginFailure.EmailNotVerified, result.Failure);
        _tokens.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithValidPayload_CreatesAccountAndSendsVerificationEmail()
    {
        string? capturedAddress = null;
        string? capturedBody = null;
        _email
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback((string to, string subject, string body, CancellationToken _) =>
            {
                capturedAddress = to;
                capturedBody = body;
            })
            .Returns(Task.CompletedTask);
        _users
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _users
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.RegisterAsync(
            new RegisterRequest("obiwan", "Obi-Wan Kenobi", "Obi.Wan@Example.com", "kenobi123"));

        Assert.NotNull(result);
        Assert.Equal("obiwan", result.Username);
        Assert.Equal("Obi-Wan Kenobi", result.DisplayName);
        Assert.Equal("obi.wan@example.com", result.Email);
        _users.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == "obi.wan@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(x => x.SendAsync("obi.wan@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("obi.wan@example.com", capturedAddress);
        Assert.NotNull(capturedBody);
        Assert.Contains("https://localhost:4200/verify-email?token=", capturedBody);
    }

    [Fact]
    public async Task RegisterAsync_WithoutDisplayName_UsesUsername()
    {
        _users
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _users
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.RegisterAsync(
            new RegisterRequest("ahsoka", null, "ahsoka@example.com", "tano12345"));

        Assert.Equal("ahsoka", result.DisplayName);
        _users.Verify(x => x.AddAsync(It.Is<User>(u => u.DisplayName == "ahsoka"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_Throws()
    {
        var existing = SeedUser("padme", "padme123", UserRole.Standard);
        _users
            .Setup(x => x.GetByUsernameAsync("padme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RegisterAsync(new RegisterRequest("padme", null, "other@example.com", "padme12345")));

        Assert.Equal(nameof(RegisterRequest.Username), exception.ParamName);
        _users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_CaseInsensitive_Throws()
    {
        var existing = SeedUser("padme", "padme123", UserRole.Standard);
        existing.Email = "PADME@EXAMPLE.COM";
        _users
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _users
            .Setup(x => x.GetByEmailAsync("padme@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RegisterAsync(new RegisterRequest("newuser", null, "  PADME@example.com ", "password123")));

        Assert.Equal(nameof(RegisterRequest.Email), exception.ParamName);
    }

    [Fact]
    public async Task RegisterAsync_WithBlankUsername_Throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RegisterAsync(new RegisterRequest("   ", null, "user@example.com", "password123")));

        Assert.Equal(nameof(RegisterRequest.Username), exception.ParamName);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmail_Throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RegisterAsync(new RegisterRequest("user", null, "not-an-email", "password123")));

        Assert.Equal(nameof(RegisterRequest.Email), exception.ParamName);
    }

    [Fact]
    public async Task RegisterAsync_WithShortPassword_Throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RegisterAsync(new RegisterRequest("user", null, "user@example.com", "12345")));

        Assert.Equal(nameof(RegisterRequest.Password), exception.ParamName);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_MarksEmailVerified()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: false);
        user.EmailVerificationTokenHash = HashToken(token);
        user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(24);
        _users
            .Setup(x => x.GetByEmailVerificationTokenHashAsync(HashToken(token), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.VerifyEmailAsync(token);

        Assert.NotNull(user.EmailVerifiedAtUtc);
        Assert.Null(user.EmailVerificationTokenHash);
        Assert.Null(user.EmailVerificationTokenExpiresAtUtc);
        _users.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithUnknownToken_Throws()
    {
        _users
            .Setup(x => x.GetByEmailVerificationTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.VerifyEmailAsync("unknown-token"));

        Assert.StartsWith("The verification link is invalid or has expired.", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_Throws()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: false);
        user.EmailVerificationTokenHash = HashToken(token);
        user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(-1);
        _users
            .Setup(x => x.GetByEmailVerificationTokenHashAsync(HashToken(token), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.VerifyEmailAsync(token));

        Assert.StartsWith("The verification link is invalid or has expired.", exception.Message);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithBlankToken_Throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.VerifyEmailAsync("  "));

        Assert.Equal("token", exception.ParamName);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_ByUsername_SendsFreshVerificationEmail()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: false);
        _users.Setup(x => x.GetByUsernameAsync("padme", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        string? capturedBody = null;
        _email
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback((string to, string subject, string body, CancellationToken _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        await _service.ResendVerificationEmailAsync("padme");

        _users.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(x => x.SendAsync("padme@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(user.EmailVerificationTokenHash);
        Assert.NotNull(user.EmailVerificationTokenExpiresAtUtc);
        Assert.Contains("https://localhost:4200/verify-email?token=", capturedBody);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_ByEmailAddress_SendsVerificationEmail()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: false);
        _users.Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _users.Setup(x => x.GetByEmailAsync("padme@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await _service.ResendVerificationEmailAsync("PADME@example.com");

        _users.Verify(x => x.GetByEmailAsync("padme@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(x => x.SendAsync("padme@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WhenAlreadyVerified_DoesNotSend()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: true);
        _users.Setup(x => x.GetByUsernameAsync("padme", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await _service.ResendVerificationEmailAsync("padme");

        _users.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WhenAccountNotFound_DoesNotSend()
    {
        _users.Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await _service.ResendVerificationEmailAsync("nobody@example.com");

        _users.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _email.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WithBlankIdentifier_Throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ResendVerificationEmailAsync("   "));

        Assert.Equal("usernameOrEmail", exception.ParamName);
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_RotatesTokenAndReturnsNewPair()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        var oldTokenValue = "old-refresh-token";
        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = oldTokenValue,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        _refreshTokens.Setup(x => x.GetByTokenAsync(oldTokenValue, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokens.Setup(x => x.GenerateToken(user)).Returns("new-access-token");
        _tokens.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");

        var result = await _service.RefreshAsync(oldTokenValue);

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.Equal("padme", result.User.Username);
        Assert.NotNull(stored.RevokedAtUtc);
        Assert.Equal("new-refresh-token", stored.ReplacedByToken);
        _refreshTokens.Verify(x => x.Update(stored), Times.Once);
        _refreshTokens.Verify(x => x.AddAsync(It.Is<RefreshToken>(rt =>
            rt.Token == "new-refresh-token" && rt.UserId == user.Id), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_WithBlankToken_Throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RefreshAsync("  "));

        Assert.Equal("refreshToken", exception.ParamName);
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownToken_Throws()
    {
        _refreshTokens
            .Setup(x => x.GetByTokenAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RefreshAsync("unknown"));

        Assert.Contains("invalid", exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedToken_RevokesAllFamilyAndThrows()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            RevokedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        _refreshTokens.Setup(x => x.GetByTokenAsync("revoked-token", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RefreshAsync("revoked-token"));

        Assert.Contains("already been revoked", exception.Message);
        _refreshTokens.Verify(x => x.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_Throws()
    {
        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "expired-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-8)
        };
        _refreshTokens.Setup(x => x.GetByTokenAsync("expired-token", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RefreshAsync("expired-token"));

        Assert.Contains("expired", exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WhenUserDeleted_Throws()
    {
        var userId = Guid.NewGuid();
        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "orphaned-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        _refreshTokens.Setup(x => x.GetByTokenAsync("orphaned-token", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RefreshAsync("orphaned-token"));

        Assert.Contains("no longer exists", exception.Message);
    }

    private User SeedUser(string username, string password, UserRole role, bool emailVerified = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = "Padmé Amidala",
            Email = $"{username}@example.com",
            EmailVerifiedAtUtc = emailVerified ? DateTime.UtcNow : null,
            PasswordHash = _hasher.HashPassword(null!, password),
            Role = role,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
