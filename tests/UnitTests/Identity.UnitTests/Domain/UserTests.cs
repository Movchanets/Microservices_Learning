using FluentAssertions;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.UnitTests.Domain;

/// <summary>
/// Unit tests for the <see cref="User"/> domain aggregate.
/// Rationale: Ensures that domain invariants, validation rules, and event generation logic operate correctly.
/// </summary>
public sealed class UserTests
{
    /// <summary>
    /// Tests the happy path for creating a <see cref="User"/>, validating that fields are normalized
    /// and the <see cref="UserRegisteredEvent"/> is properly raised.
    /// </summary>
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

    /// <summary>
    /// Tests that an <see cref="ArgumentException"/> is thrown when attempting to create a user with an invalid first name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WhenFirstNameIsNullOrWhiteSpace_ShouldThrowArgumentException(string? invalidFirstName)
    {
        var action = () => User.Create("buyer@example.com", "hashed-password", invalidFirstName!, "Doe");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("firstName");
    }

    /// <summary>
    /// Tests that an <see cref="ArgumentException"/> is thrown when attempting to create a user with an invalid last name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WhenLastNameIsNullOrWhiteSpace_ShouldThrowArgumentException(string? invalidLastName)
    {
        var action = () => User.Create("buyer@example.com", "hashed-password", "Jane", invalidLastName!);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("lastName");
    }

    /// <summary>
    /// Tests that an <see cref="ArgumentException"/> is thrown when attempting to create a user with an invalid email address.
    /// Rationale: Although email validation happens inside the <see cref="Email.Create"/> method, the static factory method <see cref="User.Create"/>
    /// propagates this exception. Testing this ensures the aggregate factory method bubbles up the error correctly.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    public void Create_WhenEmailIsInvalid_ShouldThrowArgumentException(string? invalidEmail)
    {
        var action = () => User.Create(invalidEmail!, "hashed-password", "Jane", "Doe");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }

    /// <summary>
    /// Tests that an <see cref="ArgumentException"/> is thrown when attempting to create a user with an invalid password hash.
    /// Rationale: Validates that the <see cref="PasswordHash.Create"/> validation bubbles up correctly.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WhenPasswordHashIsNullOrWhiteSpace_ShouldThrowArgumentException(string? invalidPasswordHash)
    {
        var action = () => User.Create("buyer@example.com", invalidPasswordHash!, "Jane", "Doe");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("hash");
    }

    /// <summary>
    /// Tests that a <see cref="UserRoleChangedEvent"/> is raised when the user's role is successfully changed.
    /// </summary>
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

    /// <summary>
    /// Tests the lifecycle of setting and revoking a refresh token on a <see cref="User"/> entity.
    /// </summary>
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

    /// <summary>
    /// Tests that deactivating a user correctly updates the IsActive flag to false.
    /// </summary>
    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Tests that activating a deactivated user correctly updates the IsActive flag back to true.
    /// </summary>
    [Fact]
    public void Activate_WhenUserIsDeactivated_ShouldSetIsActiveToTrue()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");
        user.Deactivate();

        user.Activate();

        user.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the FullName property computes correctly by concatenating FirstName and LastName.
    /// </summary>
    [Fact]
    public void FullName_ShouldReturnFirstNameAndLastNameWithSpace()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        user.FullName.Should().Be("Jane Doe");
    }

    /// <summary>
    /// Tests that updating the user's profile successfully updates the name fields
    /// and preserves the aggregate's identity (Id remains unchanged).
    /// </summary>
    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateNamesAndPreserveIdentity()
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");
        var originalId = user.Id;

        user.UpdateProfile("  John ", "Smith  ");

        user.Id.Should().Be(originalId);
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Smith");
    }

    /// <summary>
    /// Tests that updating a user's profile with invalid first name throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateProfile_WhenFirstNameIsInvalid_ShouldThrowArgumentException(string? invalidFirstName)
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        var action = () => user.UpdateProfile(invalidFirstName!, "Smith");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("firstName");
    }

    /// <summary>
    /// Tests that updating a user's profile with invalid last name throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateProfile_WhenLastNameIsInvalid_ShouldThrowArgumentException(string? invalidLastName)
    {
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        var action = () => user.UpdateProfile("John", invalidLastName!);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("lastName");
    }
}
