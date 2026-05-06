using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Aggregates;

/// <summary>
/// Represents a user entity and serves as the aggregate root for the Identity domain.
/// </summary>
public sealed class User : AggregateRoot
{
    /// <summary>Gets the user's email address.</summary>
    public Email Email { get; private set; } = null!;

    /// <summary>Gets the user's hashed password.</summary>
    public PasswordHash PasswordHash { get; private set; } = null!;

    /// <summary>Gets the user's first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the user's last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the role assigned to the user.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Gets the current refresh token assigned to the user.</summary>
    public RefreshToken? CurrentRefreshToken { get; private set; }

    /// <summary>Gets a value indicating whether the user is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the date and time when the user was created.</summary>
    public DateTime CreatedAt { get; private init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// Rationale: Parameterless constructor required by EF Core for object materialization.
    /// </summary>
    private User() { }

    /// <summary>
    /// Factory method to create a new user.
    /// Rationale: Encapsulates creation logic, ensures invariant validations, and raises the domain event.
    /// </summary>
    /// <param name="email">The plaintext email address.</param>
    /// <param name="passwordHash">The pre-hashed password.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="role">The user role, defaulting to Buyer.</param>
    /// <returns>A newly instantiated and validated User aggregate.</returns>
    /// <exception cref="ArgumentException">Thrown when firstName or lastName are null/whitespace.</exception>
    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role = UserRole.Buyer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var user = new User
        {
            Email = Email.Create(email),
            PasswordHash = PasswordHash.Create(passwordHash),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new UserRegisteredEvent(
            user.Id,
            user.Email.Value,
            user.Role.ToString()));

        return user;
    }

    /// <summary>
    /// Changes the role of the user.
    /// </summary>
    /// <param name="newRole">The new role to assign to the user.</param>
    public void ChangeRole(UserRole newRole)
    {
        if (Role == newRole) return;

        var oldRole = Role.ToString();
        Role = newRole;

        AddDomainEvent(new UserRoleChangedEvent(Id, oldRole, newRole.ToString()));
    }

    /// <summary>
    /// Sets a new refresh token for the user.
    /// </summary>
    /// <param name="token">The refresh token to set.</param>
    /// <exception cref="ArgumentNullException">Thrown if token is null.</exception>
    public void SetRefreshToken(RefreshToken token) =>
        CurrentRefreshToken = token ?? throw new ArgumentNullException(nameof(token));

    /// <summary>
    /// Revokes the current refresh token by setting it to null.
    /// </summary>
    public void RevokeRefreshToken() =>
        CurrentRefreshToken = null;

    /// <summary>
    /// Deactivates the user, preventing future logins.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Activates the user.
    /// </summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Gets the full name of the user.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}
