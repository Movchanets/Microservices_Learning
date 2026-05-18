using Cart.Application.Commands;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Cart.UnitTests.Application;

public class AddCartItemCommandHandlerTests
{
    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly Mock<IProductPriceRepository> _priceRepositoryMock;
    private readonly AddCartItemCommandHandler _handler;

    public AddCartItemCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _priceRepositoryMock = new Mock<IProductPriceRepository>();
        _handler = new AddCartItemCommandHandler(_repositoryMock.Object, _priceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_FirstItemToNewCart_ShouldCreateCartAndAddItem()
    {
        var buyerId = "buyer-new";
        var sku = "PROD-001";
        var price = 29.99m;

        _priceRepositoryMock.Setup(r => r.GetBySkuAsync(sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductPrice.Create(Guid.NewGuid(), sku, "Test Product", price, "USD"));

        var freshCart = new ShoppingCart(buyerId);
        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshCart);

        var command = new AddCartItemCommand(buyerId, sku, 2, "seller-1");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().Sku.Should().Be(sku);
        result.Value.Items.First().Quantity.Should().Be(2);
        result.Value.Items.First().Price.Should().Be(price);
        result.Value.Items.First().ShopId.Should().Be("seller-1");
        _repositoryMock.Verify(r => r.SaveCartAsync(freshCart, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AddSameSkuTwice_ShouldIncrementQuantity()
    {
        var buyerId = "buyer-existing";
        var sku = "PROD-002";
        var price = 15.50m;

        _priceRepositoryMock.Setup(r => r.GetBySkuAsync(sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductPrice.Create(Guid.NewGuid(), sku, "Another Product", price, "USD"));

        var existingCart = new ShoppingCart(buyerId);
        existingCart.AddItem(sku, 1, price, "seller-2");
        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCart);

        var command = new AddCartItemCommand(buyerId, sku, 3, "seller-2");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().Quantity.Should().Be(4);
        _repositoryMock.Verify(r => r.SaveCartAsync(existingCart, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AddDifferentSkus_ShouldKeepBothItems()
    {
        var buyerId = "buyer-multi";

        _priceRepositoryMock.Setup(r => r.GetBySkuAsync("SKU-A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductPrice.Create(Guid.NewGuid(), "SKU-A", "Product A", 10m, "USD"));
        _priceRepositoryMock.Setup(r => r.GetBySkuAsync("SKU-B", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductPrice.Create(Guid.NewGuid(), "SKU-B", "Product B", 20m, "USD"));

        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-A", 1, 10m);
        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(buyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new AddCartItemCommand(buyerId, "SKU-B", 5, "seller-3");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().Contain(i => i.Sku == "SKU-A" && i.Quantity == 1);
        result.Value.Items.Should().Contain(i => i.Sku == "SKU-B" && i.Quantity == 5);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _priceRepositoryMock.Setup(r => r.GetBySkuAsync("MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductPrice?)null);

        var command = new AddCartItemCommand("buyer-1", "MISSING", 1);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("MISSING");
        _repositoryMock.Verify(r => r.GetOrCreateTrackedCartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveCartAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
