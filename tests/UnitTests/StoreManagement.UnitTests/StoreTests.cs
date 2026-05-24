using FluentAssertions;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;
using StoreManagement.Domain.Events;

namespace StoreManagement.UnitTests;

public class StoreTests
{
    [Fact]
    public void Create_WithValidData_ReturnsStore()
    {
        // Arrange & Act
        var store = Store.Create("seller-123", "Test Store", "A great store description");

        // Assert
        store.Should().NotBeNull();
        store.SellerId.Should().Be("seller-123");
        store.Name.Should().Be("Test Store");
        store.Description.Should().Be("A great store description");
        store.VerificationStatus.Should().Be(VerificationStatus.Pending);
        store.LogoUrl.Should().BeNull();
        store.RejectionReason.Should().BeNull();
        store.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithValidData_RaisesStoreCreatedDomainEvent()
    {
        // Arrange & Act
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Assert
        store.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StoreCreatedDomainEvent>()
            .Which.Should().Match<StoreCreatedDomainEvent>(e =>
                e.StoreId == store.Id &&
                e.SellerId == "seller-123" &&
                e.StoreName == "Test Store");
    }

    [Fact]
    public void Create_WithEmptySellerId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Store.Create("", "Test Store", "Description");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Store.Create("seller-123", "", "Description");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyDescription_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Store.Create("seller-123", "Test Store", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesProperties()
    {
        // Arrange
        var store = Store.Create("seller-123", "Old Name", "Old Description");

        // Act
        store.UpdateDetails("New Name", "New Description");

        // Assert
        store.Name.Should().Be("New Name");
        store.Description.Should().Be("New Description");
        store.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Act & Assert
        var act = () => store.UpdateDetails("", "New Description");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetLogo_WithValidUrl_SetsLogoUrl()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Act
        store.SetLogo("https://example.com/logo.png");

        // Assert
        store.LogoUrl.Should().Be("https://example.com/logo.png");
        store.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetLogo_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Act & Assert
        var act = () => store.SetLogo("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Verify_WhenPending_SetsVerified()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Act
        store.Verify();

        // Assert
        store.VerificationStatus.Should().Be(VerificationStatus.Verified);
        store.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        store.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void Verify_WhenPending_RaisesStoreVerifiedDomainEvent()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Act
        store.Verify();

        // Assert
        store.DomainEvents.Should().Contain(e => e is StoreVerifiedDomainEvent)
            .Which.Should().BeOfType<StoreVerifiedDomainEvent>()
            .Which.SellerId.Should().Be("seller-123");
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");
        store.Verify();

        // Act & Assert
        var act = () => store.Verify();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already verified*");
    }

    [Fact]
    public void Reject_WhenPending_SetsRejectedWithReason()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Act
        store.Reject("Incomplete documentation");

        // Assert
        store.VerificationStatus.Should().Be(VerificationStatus.Rejected);
        store.RejectionReason.Should().Be("Incomplete documentation");
        store.VerifiedAt.Should().BeNull();
    }

    [Fact]
    public void Reject_WithEmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Act & Assert
        var act = () => store.Reject("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reject_WhenAlreadyVerified_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");
        store.Verify();

        // Act & Assert
        var act = () => store.Reject("some reason");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot reject*verified*");
    }

    [Fact]
    public void IsVerified_WhenPending_ReturnsFalse()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");

        // Assert
        store.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void IsVerified_WhenRejected_ReturnsFalse()
    {
        // Arrange
        var store = Store.Create("seller-123", "Test Store", "Description");
        store.Reject("reason");

        // Assert
        store.IsVerified.Should().BeFalse();
    }
}
