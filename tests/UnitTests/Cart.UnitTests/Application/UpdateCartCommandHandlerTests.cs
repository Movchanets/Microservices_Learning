using Cart.Application.Commands;
using Cart.Domain.Aggregates;
using FluentAssertions;
using Moq;

namespace Cart.UnitTests.Application;

public class UpdateCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly UpdateCartCommandHandler _handler;

    public UpdateCartCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _handler = new UpdateCartCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldClearAndAddItemsAndSave()
    {
        // Arrange
        var buyerId = "buyer-1";
        var existingCart = new ShoppingCart(buyerId);
        existingCart.AddItem("old-sku", 1);

        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCart);

        var newItems = new List<CartItemDto>
        {
            new CartItemDto("new-sku-1", 2, 10m, "seller-A"),
            new CartItemDto("new-sku-2", 3, 20m, "seller-B")
        };
        var command = new UpdateCartCommand(buyerId, newItems);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Should().Contain(i => i.Sku == "new-sku-1" && i.Quantity == 2 && i.SellerId == "seller-A");
        result.Value.Items.Should().Contain(i => i.Sku == "new-sku-2" && i.Quantity == 3 && i.SellerId == "seller-B");
        result.Value.Items.Should().NotContain(i => i.Sku == "old-sku");

        _repositoryMock.Verify(r => r.SaveCartAsync(existingCart, It.IsAny<CancellationToken>()), Times.Once);
    }
}
