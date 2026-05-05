using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Aggregates;

public sealed class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public PasswordHash PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public RefreshToken? CurrentRefreshToken { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private init; }

    // EF Core constructor
    private User() { }

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

    public void ChangeRole(UserRole newRole)
    {
        if (Role == newRole) return;

        var oldRole = Role.ToString();
        Role = newRole;

        AddDomainEvent(new UserRoleChangedEvent(Id, oldRole, newRole.ToString()));
    }

    public void SetRefreshToken(RefreshToken token) =>
        CurrentRefreshToken = token ?? throw new ArgumentNullException(nameof(token));

    public void RevokeRefreshToken() =>
        CurrentRefreshToken = null;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public string FullName => $"{FirstName} {LastName}";
}
