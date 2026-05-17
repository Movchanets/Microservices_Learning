using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Cart;
using BuildingBlocks.SharedContracts.Dtos;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Ordering.Domain.Aggregates;
using Ordering.Domain.ValueObjects;
using Ordering.Infrastructure.Messaging.Consumers;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that the OrderSubmittedConsumer correctly
/// handles the OrderSubmittedEvent from the Cart microservice.
///
/// Tests idempotency, field mapping, and proper domain entity creation.
/// </summary>
public class OrderingConsumerContractTests
{
    [Fact]
    public async Task OrderSubmittedEvent_Contract_ShouldCreateOrderEntity()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var buyerId = "buyer-order-001";
        var items = new List<OrderItemContract>
        {
            new("SKU-A", 3, 15.99m, "seller-1"),
            new("SKU-B", 1, 29.99m, "seller-2")
        };

        var @event = new OrderSubmittedEvent(
            CorrelationId: correlationId,
            BuyerId: buyerId,
            Items: items,
            Timestamp: DateTime.UtcNow,
            ShippingAddressLine1: "456 Oak Ave",
            ShippingAddressLine2: null,
            ShippingCity: "Portland",
            ShippingState: "OR",
            ShippingPostalCode: "97201",
            ShippingCountry: "US");

        Order? capturedOrder = null;
        var repositoryMock = new Mock<IOrderRepository>();
        repositoryMock
            .Setup(x => x.GetByIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        repositoryMock
            .Setup(x => x.Add(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var consumeContext = new Mock<ConsumeContext<OrderSubmittedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new OrderSubmittedConsumer(
            repositoryMock.Object, uowMock.Object, Mock.Of<ILogger<OrderSubmittedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - verify the order entity contract
        capturedOrder.Should().NotBeNull();
        capturedOrder!.BuyerId.Should().Be(buyerId);
        capturedOrder.Items.Should().HaveCount(2);
        capturedOrder.Items.Should().Contain(i =>
            i.Sku == "SKU-A" && i.Quantity == 3 && i.UnitPrice == 15.99m && i.SellerId == "seller-1");
        capturedOrder.Items.Should().Contain(i =>
            i.Sku == "SKU-B" && i.Quantity == 1 && i.UnitPrice == 29.99m && i.SellerId == "seller-2");
        capturedOrder.ShippingAddress.Should().NotBeNull();
        capturedOrder.ShippingAddress.City.Should().Be("Portland");
        capturedOrder.ShippingAddress.State.Should().Be("OR");
        capturedOrder.ShippingAddress.Country.Should().Be("US");
    }

    [Fact]
    public async Task OrderSubmittedEvent_Contract_ShouldBeIdempotent()
    {
        // Arrange - order already exists (saga may replay)
        var correlationId = Guid.NewGuid();

        var @event = new OrderSubmittedEvent(
            CorrelationId: correlationId,
            BuyerId: "buyer-idempotent",
            Items: [new OrderItemContract("SKU-1", 1, 10m)],
            Timestamp: DateTime.UtcNow);

        var existingOrder = Order.Create("buyer-idempotent",
            Address.FromShipping("123 St", null, "City", "ST", "12345", "US"));

        var repositoryMock = new Mock<IOrderRepository>();
        repositoryMock
            .Setup(x => x.GetByIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        var uowMock = new Mock<IUnitOfWork>();

        var consumeContext = new Mock<ConsumeContext<OrderSubmittedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new OrderSubmittedConsumer(
            repositoryMock.Object, uowMock.Object, Mock.Of<ILogger<OrderSubmittedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - should not add or save
        repositoryMock.Verify(x => x.Add(It.IsAny<Order>()), Times.Never);
        uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OrderSubmittedEvent_Contract_ShouldHandleMinimalShippingAddress()
    {
        // Arrange - only required fields
        var correlationId = Guid.NewGuid();
        var @event = new OrderSubmittedEvent(
            CorrelationId: correlationId,
            BuyerId: "buyer-minimal",
            Items: [new OrderItemContract("SKU-1", 1, 5m)],
            Timestamp: DateTime.UtcNow,
            ShippingAddressLine1: "789 Elm St",
            ShippingAddressLine2: null,
            ShippingCity: "Denver",
            ShippingState: "CO",
            ShippingPostalCode: "80201",
            ShippingCountry: "US");

        Order? capturedOrder = null;
        var repositoryMock = new Mock<IOrderRepository>();
        repositoryMock
            .Setup(x => x.GetByIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        repositoryMock
            .Setup(x => x.Add(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var consumeContext = new Mock<ConsumeContext<OrderSubmittedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new OrderSubmittedConsumer(
            repositoryMock.Object, uowMock.Object, Mock.Of<ILogger<OrderSubmittedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert
        capturedOrder.Should().NotBeNull();
        capturedOrder!.ShippingAddress.Street.Should().Be("789 Elm St");
        capturedOrder.ShippingAddress.City.Should().Be("Denver");
    }
}
