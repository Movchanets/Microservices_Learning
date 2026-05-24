using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.StoreManagement;
using FluentAssertions;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using Identity.Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Identity consumers correctly handle
/// cross-service events (StoreVerified, UserRegistered).
///
/// Tests the message contract between StoreManagement/Identity services.
/// </summary>
public class IdentityContractTests
{
    [Fact]
    public async Task StoreVerifiedEvent_Contract_ShouldUpgradeUserToSellerRole()
    {
        // Arrange - StoreManagement publishes this when store is verified
        var sellerId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var @event = new StoreVerifiedIntegrationEvent(
            storeId, sellerId.ToString(), DateTime.UtcNow);

        var user = User.Create("seller@example.com", "hashed_pw", "John", "Doe", UserRole.Buyer);

        var repoMock = new Mock<IUserRepository>();
        repoMock
            .Setup(x => x.GetByIdAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var consumeContext = new Mock<ConsumeContext<StoreVerifiedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new StoreVerifiedConsumer(
            repoMock.Object, uowMock.Object, Mock.Of<ILogger<StoreVerifiedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert
        user.Role.Should().HaveFlag(UserRole.Seller);
        uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StoreVerifiedEvent_Contract_ShouldHandleInvalidSellerId()
    {
        // Arrange - invalid GUID format
        var @event = new StoreVerifiedIntegrationEvent(
            Guid.NewGuid(), "not-a-guid", DateTime.UtcNow);

        var repoMock = new Mock<IUserRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        var consumeContext = new Mock<ConsumeContext<StoreVerifiedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new StoreVerifiedConsumer(
            repoMock.Object, uowMock.Object, Mock.Of<ILogger<StoreVerifiedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - should not attempt to find user
        repoMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        uowMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StoreVerifiedEvent_Contract_ShouldHandleNonExistentUser()
    {
        // Arrange - user not found
        var sellerId = Guid.NewGuid();
        var @event = new StoreVerifiedIntegrationEvent(
            Guid.NewGuid(), sellerId.ToString(), DateTime.UtcNow);

        var repoMock = new Mock<IUserRepository>();
        repoMock
            .Setup(x => x.GetByIdAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var uowMock = new Mock<IUnitOfWork>();

        var consumeContext = new Mock<ConsumeContext<StoreVerifiedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new StoreVerifiedConsumer(
            repoMock.Object, uowMock.Object, Mock.Of<ILogger<StoreVerifiedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - should not save
        uowMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StoreVerifiedEvent_Contract_ShouldPreserveExistingUserData()
    {
        // Arrange - verify role change doesn't affect other user data
        var sellerId = Guid.NewGuid();
        var @event = new StoreVerifiedIntegrationEvent(
            Guid.NewGuid(), sellerId.ToString(), DateTime.UtcNow);

        var user = User.Create("preserve@example.com", "hashed_pw", "Jane", "Smith", UserRole.Buyer);
        var originalEmail = user.Email.Value;
        var originalFirstName = user.FirstName;

        var repoMock = new Mock<IUserRepository>();
        repoMock
            .Setup(x => x.GetByIdAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var consumeContext = new Mock<ConsumeContext<StoreVerifiedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new StoreVerifiedConsumer(
            repoMock.Object, uowMock.Object, Mock.Of<ILogger<StoreVerifiedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - only role changed
        user.Role.Should().HaveFlag(UserRole.Seller);
        user.Email.Value.Should().Be(originalEmail);
        user.FirstName.Should().Be(originalFirstName);
    }
}
