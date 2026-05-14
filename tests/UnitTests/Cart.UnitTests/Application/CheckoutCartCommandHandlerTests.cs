using BuildingBlocks.SharedContracts.Events.Cart;
using Cart.Application.Commands;
using Cart.Domain.Aggregates;
using FluentAssertions;
using MassTransit;
using Moq;

namespace Cart.UnitTests.Application;

public class CheckoutCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly CheckoutCartCommandHandler _handler;

    public CheckoutCartCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _handler = new CheckoutCartCommandHandler(_repositoryMock.Object, _publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCartIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var buyerId = "buyer-1";
        var emptyCart = new ShoppingCart(buyerId);
        _repositoryMock.Setup(r => r.GetCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCart);

        var command = new CheckoutCartCommand(buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cart is empty.");

        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<OrderSubmittedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteCartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCartHasItems_ShouldPublishEventDeleteCartAndReturnSuccess()
    {
        // Arrange
        var buyerId = "buyer-1";
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("sku-1", 2);
        cart.AddItem("sku-2", 3);
        _repositoryMock.Setup(r => r.GetCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new CheckoutCartCommand(buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CorrelationId.Should().NotBeEmpty();

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<OrderSubmittedEvent>(e =>
                e.BuyerId == buyerId &&
                e.Items.Count == 2 &&
                e.CorrelationId == result.Value.CorrelationId),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.DeleteCartAsync(buyerId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
