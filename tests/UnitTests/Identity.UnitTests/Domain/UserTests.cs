using FluentAssertions;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.UnitTests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Create_ShouldNormalizeAndTrimFields_AndRaiseUserRegisteredEvent()
    {
        var user = User.Create("  BUYER@Example.Com  ", "hashed-password", "  Jane ", " Doe  ");

        user.Email.Value.Should().Be("buyer@example.com");
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Doe");
        user.Role.Should().Be(UserRole.Buyer);
        user.IsActive.Should().BeTrue();

        var domainEvent = user.DomainEvents.Should().ContainSingle().Subject;
        var registeredEvent = domainEvent.Should().BeOfType<UserRegisteredEvent>().Subject;
        registeredEvent.UserId.Should().Be(user.Id);
        registeredEvent.Email.Should().Be("buyer@example.com");
        registeredEvent.Role.Should().Be(nameof(UserRole.Buyer));
    }

    [Fact]
    public void ChangeRole_WhenRoleChanges_ShouldRaiseUserRoleChangedEvent()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        user.ChangeRole(UserRole.Seller);

        user.Role.Should().Be(UserRole.Seller);
        var roleChangedEvent = user.DomainEvents
            .Should().ContainSingle(x => x is UserRoleChangedEvent)
            .Which
            .Should().BeOfType<UserRoleChangedEvent>()
            .Subject;
        roleChangedEvent.UserId.Should().Be(user.Id);
        roleChangedEvent.OldRole.Should().Be(nameof(UserRole.Buyer));
        roleChangedEvent.NewRole.Should().Be(nameof(UserRole.Seller));
    }

    [Fact]
    public void RefreshTokenLifecycle_ShouldSetAndRevokeCurrentRefreshToken()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");
        var refreshToken = RefreshToken.Create("refresh-token", TimeSpan.FromMinutes(5));

        user.SetRefreshToken(refreshToken);
        user.CurrentRefreshToken.Should().NotBeNull();
        user.CurrentRefreshToken!.Token.Should().Be("refresh-token");

        user.RevokeRefreshToken();
        user.CurrentRefreshToken.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_WhenUserIsDeactivated_ShouldSetIsActiveToTrue()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");
        user.Deactivate();

        user.Activate();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void FullName_ShouldReturnFirstNameAndLastNameWithSpace()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        user.FullName.Should().Be("Jane Doe");
    }
}
