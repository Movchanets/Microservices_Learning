namespace Identity.Domain.Enums;

/// <summary>
/// Defines the different roles a user can hold within the system.
/// </summary>
public enum UserRole
{
    /// <summary>A standard buyer account.</summary>
    Buyer = 0,

    /// <summary>A seller account with privileges to manage products.</summary>
    Seller = 1,

    /// <summary>An administrative account with full system access.</summary>
    Admin = 2
}
