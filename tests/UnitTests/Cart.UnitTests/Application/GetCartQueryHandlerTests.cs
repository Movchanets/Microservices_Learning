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
        Guid? buyerId = Guid.NewGuid();
        var cart = new ShoppingCart(buyerId);
        var productId = Guid.NewGuid();
        var storeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        cart.AddItem(productId, 1, storeId, 10m);
        _repositoryMock.Setup(r => r.GetCartAsync(buyerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartQuery(buyerId, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<CartResponse>();
        result.Value.BuyerId.Should().Be(buyerId);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().ProductId.Should().Be(productId);
        result.Value.Items.First().Quantity.Should().Be(1);
        result.Value.TotalPrice.Should().Be(10m);
        result.Value.TotalItems.Should().Be(1);
    }
}
