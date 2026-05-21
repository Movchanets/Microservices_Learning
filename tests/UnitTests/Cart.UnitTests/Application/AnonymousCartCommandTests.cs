using Cart.Application.Commands;
using Cart.Application.Dtos;
using Cart.Application.Queries;
using Cart.Domain.Aggregates;
using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Cart.UnitTests.Application;

/// <summary>
/// Tests for anonymous cart operations (BuyerId = null, CartId used for lookup).
/// </summary>
public class AnonymousCartCommandTests
{
    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly Mock<IProductPriceRepository> _priceRepositoryMock;

    public AnonymousCartCommandTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _priceRepositoryMock = new Mock<IProductPriceRepository>();
    }

    // ── GetCartQuery ──

    [Fact]
    public async Task GetCartQuery_NullBuyerId_WithCartId_ShouldLookupByCartId()
    {
        var cartId = Guid.NewGuid();
        var cart = new ShoppingCart(null);
        typeof(ShoppingCart).GetProperty("Id")!.SetValue(cart, cartId);
        cart.AddItem(TestProductId, 2, TestStoreId, 10m);

        _repositoryMock.Setup(r => r.GetCartAsync(null, cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler = new GetCartQueryHandler(_repositoryMock.Object);
        var result = await handler.Handle(new GetCartQuery(null, cartId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuyerId.Should().BeNull();
        result.Value.CartId.Should().Be(cartId);
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetCartQuery_NullBuyerId_NullCartId_ShouldReturnEmptyCart()
    {
        var emptyCart = new ShoppingCart(null);
        _repositoryMock.Setup(r => r.GetCartAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCart);

        var handler = new GetCartQueryHandler(_repositoryMock.Object);
        var result = await handler.Handle(new GetCartQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuyerId.Should().BeNull();
        result.Value.Items.Should().BeEmpty();
    }

    // ── AddCartItemCommand ──

    [Fact]
    public async Task AddCartItem_NullBuyerId_ShouldCreateAnonymousCartAndAddItem()
    {
        var price = 29.99m;
        _priceRepositoryMock.Setup(r => r.GetByIdAsync(TestProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductPrice.Create(TestProductId, "PROD-001", "Test Product", price, "USD", TestStoreId));

        var anonCart = new ShoppingCart(null);
        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anonCart);

        var handler = new AddCartItemCommandHandler(_repositoryMock.Object, _priceRepositoryMock.Object);
        var result = await handler.Handle(new AddCartItemCommand(null, null, TestProductId, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuyerId.Should().BeNull();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().ProductId.Should().Be(TestProductId);
        result.Value.Items.First().Quantity.Should().Be(1);

        _repositoryMock.Verify(r => r.SaveCartAsync(anonCart, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCartItem_NullBuyerId_WithCartId_ShouldUseCartIdLookup()
    {
        var cartId = Guid.NewGuid();
        var price = 15m;
        _priceRepositoryMock.Setup(r => r.GetByIdAsync(TestProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductPrice.Create(TestProductId, "PROD-001", "Test", price, "USD", TestStoreId));

        var anonCart = new ShoppingCart(null);
        typeof(ShoppingCart).GetProperty("Id")!.SetValue(anonCart, cartId);
        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(null, cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anonCart);

        var handler = new AddCartItemCommandHandler(_repositoryMock.Object, _priceRepositoryMock.Object);
        var result = await handler.Handle(new AddCartItemCommand(null, cartId, TestProductId, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CartId.Should().Be(cartId);
        _repositoryMock.Verify(r => r.GetOrCreateTrackedCartAsync(null, cartId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateCartItemCommand ──

    [Fact]
    public async Task UpdateCartItem_NullBuyerId_ShouldUpdateAnonymousCart()
    {
        var cartId = Guid.NewGuid();
        var anonCart = new ShoppingCart(null);
        typeof(ShoppingCart).GetProperty("Id")!.SetValue(anonCart, cartId);
        anonCart.AddItem(TestProductId, 1, TestStoreId, 10m);

        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(null, cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anonCart);

        var handler = new UpdateCartItemCommandHandler(_repositoryMock.Object);
        var result = await handler.Handle(new UpdateCartItemCommand(null, cartId, TestProductId, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().Quantity.Should().Be(5);
    }

    // ── RemoveCartItemCommand ──

    [Fact]
    public async Task RemoveCartItem_NullBuyerId_ShouldRemoveFromAnonymousCart()
    {
        var cartId = Guid.NewGuid();
        var anonCart = new ShoppingCart(null);
        typeof(ShoppingCart).GetProperty("Id")!.SetValue(anonCart, cartId);
        anonCart.AddItem(TestProductId, 2, TestStoreId, 10m);

        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(null, cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anonCart);

        var handler = new RemoveCartItemCommandHandler(_repositoryMock.Object);
        var result = await handler.Handle(new RemoveCartItemCommand(null, cartId, TestProductId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── DeleteCartCommand ──

    [Fact]
    public async Task DeleteCart_NullBuyerId_WithCartId_ShouldDeleteByCartId()
    {
        var cartId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.DeleteCartAsync(null, cartId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteCartCommandHandler(_repositoryMock.Object);
        var result = await handler.Handle(new DeleteCartCommand(null, cartId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteCartAsync(null, cartId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateCartCommand ──

    [Fact]
    public async Task UpdateCart_NullBuyerId_ShouldReplaceAnonymousCartItems()
    {
        var cartId = Guid.NewGuid();
        var anonCart = new ShoppingCart(null);
        typeof(ShoppingCart).GetProperty("Id")!.SetValue(anonCart, cartId);
        anonCart.AddItem(Guid.NewGuid(), 1, TestStoreId, 5m); // old item

        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(null, cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anonCart);

        var newItems = new List<CartItemDto>
        {
            new(TestProductId, 3, 15m, TestStoreId)
        };

        var handler = new UpdateCartCommandHandler(_repositoryMock.Object);
        var result = await handler.Handle(new UpdateCartCommand(null, cartId, newItems), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().ProductId.Should().Be(TestProductId);
        result.Value.Items.First().Quantity.Should().Be(3);
    }

    // ── CartResponse includes CartId ──

    [Fact]
    public async Task CartResponse_ShouldIncludeCartId_ForAnonymousCart()
    {
        var cartId = Guid.NewGuid();
        var anonCart = new ShoppingCart(null);
        typeof(ShoppingCart).GetProperty("Id")!.SetValue(anonCart, cartId);

        _repositoryMock.Setup(r => r.GetCartAsync(null, cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anonCart);

        var handler = new GetCartQueryHandler(_repositoryMock.Object);
        var result = await handler.Handle(new GetCartQuery(null, cartId), CancellationToken.None);

        result.Value.CartId.Should().Be(cartId);
        result.Value.BuyerId.Should().BeNull();
    }
}
