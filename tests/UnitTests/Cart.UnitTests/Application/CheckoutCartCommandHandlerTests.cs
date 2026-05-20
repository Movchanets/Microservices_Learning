using BuildingBlocks.SharedContracts.Events.Cart;
using Cart.Application.Commands;
using Cart.Domain.Aggregates;
using FluentAssertions;
using MassTransit;
using Moq;

namespace Cart.UnitTests.Application;

public class CheckoutCartCommandHandlerTests
{
    private static readonly Guid TestStoreId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestStoreId2 = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TestProductId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestProductId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

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
        var buyerId = "buyer-1";
        var emptyCart = new ShoppingCart(buyerId);
        _repositoryMock.Setup(r => r.GetCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCart);

        var command = new CheckoutCartCommand(buyerId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cart is empty.");
    }

    [Fact]
    public async Task Handle_WhenCartHasItems_ShouldPublishEventDeleteCartAndReturnSuccess()
    {
        var buyerId = "buyer-1";
        var cart = new ShoppingCart(buyerId);
        cart.AddItem(TestProductId1, 2, TestStoreId1, 10m);
        cart.AddItem(TestProductId2, 3, TestStoreId2, 20m);
        _repositoryMock.Setup(r => r.GetCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new CheckoutCartCommand(buyerId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<OrderSubmittedEvent>(e =>
                e.BuyerId == buyerId &&
                e.Items.Count == 2 &&
                e.Items.Any(i => i.StoreId == TestStoreId1) &&
                e.Items.Any(i => i.StoreId == TestStoreId2)),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.DeleteCartAsync(buyerId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
