namespace Identity.Domain.Enums;

/// <summary>
/// Defines the different roles a user can hold within the system.
/// Bit flags allow a user to hold multiple roles simultaneously (e.g., Admin + Seller).
/// </summary>
[Flags]
public enum UserRole
{
    /// <summary>No roles assigned.</summary>
    None = 0,

    /// <summary>A standard buyer account.</summary>
    Buyer = 1,

    /// <summary>A seller account with privileges to manage products.</summary>
    Seller = 2,

    /// <summary>An administrative account with full system access.</summary>
    Admin = 4,

    /// <summary>Convenience: all roles combined.</summary>
    All = Buyer | Seller | Admin
}
