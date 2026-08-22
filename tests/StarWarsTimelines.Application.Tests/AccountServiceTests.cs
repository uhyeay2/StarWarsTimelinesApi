using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Tests;

public sealed class AccountServiceTests
{
    private const string VerificationUrl = "https://localhost:4200/verify-email";

    private readonly Mock<IUserRepository> _users;
    private readonly Mock<IEmailSender> _email;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly AccountService _service;
    private readonly PasswordHasher<object> _hasher = new();

    public AccountServiceTests()
    {
        _users = new Mock<IUserRepository>();
        _email = new Mock<IEmailSender>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new AccountService(
            _users.Object,
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
            _unitOfWork.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsAccountWithEmailAndVerificationState()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: true);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.GetAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("padme", result.Username);
        Assert.Equal("Padmé Amidala", result.DisplayName);
        Assert.Equal("padme@example.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Equal(UserRole.Standard, result.Role);
    }

    [Fact]
    public async Task GetAsync_WithUnknownUser_ReturnsNull()
    {
        _users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _service.GetAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_UpdatesTheStoredName()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.UpdateDisplayNameAsync(user.Id, "  Queen Amidala  ");

        Assert.NotNull(result);
        Assert.Equal("Queen Amidala", result.DisplayName);
        Assert.Equal("Queen Amidala", user.DisplayName);
        _users.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_WithBlankName_Throws()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateDisplayNameAsync(user.Id, "   "));

        Assert.Equal("displayName", exception.ParamName);
        _users.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_WithUnknownUser_ReturnsNull()
    {
        _users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateDisplayNameAsync(Guid.NewGuid(), "Queen Amidala");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateEmailAsync_ChangesEmailAndEmailsFreshVerificationLink()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: true);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(x => x.GetByEmailAsync("queen@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
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

        var result = await _service.UpdateEmailAsync(user.Id, "  QUEEN@example.com ");

        Assert.NotNull(result);
        Assert.Equal("queen@example.com", result.Email);
        Assert.False(result.EmailVerified);
        Assert.Equal("queen@example.com", user.Email);
        Assert.Null(user.EmailVerifiedAtUtc);
        Assert.NotNull(user.EmailVerificationTokenHash);
        Assert.NotNull(user.EmailVerificationTokenExpiresAtUtc);
        _users.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(x => x.SendAsync("queen@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("queen@example.com", capturedAddress);
        Assert.NotNull(capturedBody);
        Assert.Contains("https://localhost:4200/verify-email?token=", capturedBody);
    }

    [Fact]
    public async Task UpdateEmailAsync_WithUnchangedEmail_DoesNotSendOrSave()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard, emailVerified: true);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.UpdateEmailAsync(user.Id, "PADME@example.com");

        Assert.NotNull(result);
        Assert.Equal("padme@example.com", result.Email);
        Assert.True(result.EmailVerified);
        _users.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEmailAsync_WithDuplicateEmail_Throws()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        var other = SeedUser("luke", "luke123", UserRole.Standard);
        other.Email = "taken@example.com";
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(x => x.GetByEmailAsync("taken@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(other);

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException>(() =>
            _service.UpdateEmailAsync(user.Id, "taken@example.com"));

        Assert.Equal("email", exception.ParamName);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEmailAsync_WithInvalidEmail_Throws()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateEmailAsync(user.Id, "not-an-email"));

        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public async Task UpdateEmailAsync_WithUnknownUser_ReturnsNull()
    {
        _users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _service.UpdateEmailAsync(Guid.NewGuid(), "queen@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithCorrectCurrentPassword_ChangesPassword()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        var originalHash = user.PasswordHash;
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.UpdatePasswordAsync(user.Id, "padme123", "noblequeen1");

        Assert.NotNull(result);
        Assert.NotEqual(originalHash, user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(null!, user.PasswordHash, "noblequeen1"));
        _users.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithWrongCurrentPassword_Throws()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        var originalHash = user.PasswordHash;
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdatePasswordAsync(user.Id, "wrong-password", "noblequeen1"));

        Assert.Equal("currentPassword", exception.ParamName);
        Assert.Equal(originalHash, user.PasswordHash);
        _users.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithShortNewPassword_Throws()
    {
        var user = SeedUser("padme", "padme123", UserRole.Standard);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdatePasswordAsync(user.Id, "padme123", "123"));

        Assert.Equal("newPassword", exception.ParamName);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithUnknownUser_ReturnsNull()
    {
        _users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _service.UpdatePasswordAsync(Guid.NewGuid(), "padme123", "noblequeen1");

        Assert.Null(result);
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
}
