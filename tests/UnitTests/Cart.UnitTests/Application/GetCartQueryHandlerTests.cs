using Cart.Application.Dtos;
using Cart.Application.Queries;
using Cart.Domain.Aggregates;
using FluentAssertions;
using Moq;

namespace Cart.UnitTests.Application;

public class GetCartQueryHandlerTests
{
    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly GetCartQueryHandler _handler;

    public GetCartQueryHandlerTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _handler = new GetCartQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCartResponseFromRepository()
    {
        // Arrange
        var buyerId = "buyer-1";
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("sku-1", 1, 10m);
        _repositoryMock.Setup(r => r.GetCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartQuery(buyerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<CartResponse>();
        result.Value.BuyerId.Should().Be(buyerId);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().Sku.Should().Be("sku-1");
        result.Value.Items.First().Quantity.Should().Be(1);
        result.Value.TotalPrice.Should().Be(10m);
        result.Value.TotalItems.Should().Be(1);
    }
}
