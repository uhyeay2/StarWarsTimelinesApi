using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents an account that can authenticate with the API and track source materials in a personal library.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique login name used to authenticate the user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name shown in the user interface.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized email address of the user, used for verification and password recovery.
    /// </summary>
    /// <remarks>
    /// Emails are unique per account and are normalized (trimmed and lower-cased) by the application before they
    /// are persisted, so comparisons are case-insensitive.
    /// </remarks>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp at which the user's email address was verified, or <c>null</c> while the
    /// account is still unverified.
    /// </summary>
    /// <remarks>Accounts cannot log in until this value is set.</remarks>
    public DateTime? EmailVerifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the email verification token issued to the user, or <c>null</c> when no
    /// verification is pending.
    /// </summary>
    /// <remarks>Only the hash is stored so the raw token is never persisted.</remarks>
    public string? EmailVerificationTokenHash { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the pending email verification token expires, or <c>null</c> when
    /// no verification is pending.
    /// </summary>
    public DateTime? EmailVerificationTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the hash of the user's password, produced by <c>PasswordHasher&lt;object&gt;</c>.
    /// </summary>
    /// <remarks>The plain-text password is never stored.</remarks>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authorization role that determines which API operations the user may perform.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the account was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the collection of library items (tracked source materials) owned by this user.
    /// </summary>
    /// <remarks>
    /// This is the owning side of the relationship and is never loaded implicitly; it must be included explicitly
    /// when needed. Library reads go through <c>IUserSourceMaterialRepository</c> instead.
    /// </remarks>
    public ICollection<UserSourceMaterial> UserSourceMaterials { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of per-unit progress records owned by this user.
    /// </summary>
    /// <remarks>
    /// This is the owning side of the relationship and is never loaded implicitly; it must be included explicitly
    /// when needed. Unit progress reads go through <c>IUserSourceMaterialUnitRepository</c> instead.
    /// </remarks>
    public ICollection<UserSourceMaterialUnit> UserSourceMaterialUnits { get; set; } = [];
}
