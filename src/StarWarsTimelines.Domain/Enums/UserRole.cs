namespace StarWarsTimelines.Domain.Enums;

/// <summary>
/// Specifies the authorization role of a user account.
/// </summary>
public enum UserRole
{
    /// <summary>A regular user who can manage their own library but cannot modify the catalog.</summary>
    Standard,

    /// <summary>A user who can also create, update, and delete catalog entries.</summary>
    Admin
}
