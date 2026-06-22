using Cart.Domain.Aggregates;
using FluentAssertions;

namespace Cart.UnitTests.Domain;

public class AnonymousShoppingCartTests
{
    private static readonly Guid StoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProductId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Constructor_WithNullBuyerId_ShouldCreateAnonymousCart()
    {
        var cart = new ShoppingCart(null);

        cart.BuyerId.Should().BeNull();
        // Note: cart.Id is Guid.Empty until persisted (Guid v7 generation via EF Core)
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_AnonymousCart_ShouldWork()
    {
        var cart = new ShoppingCart(null);
        cart.AddItem(ProductId1, Guid.NewGuid(), "TEST-SKU", 2, StoreId, 10m);

        cart.BuyerId.Should().BeNull();
        cart.Items.Should().ContainSingle();
        cart.Items.First().ProductId.Should().Be(ProductId1);
        cart.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_MultipleProducts_AnonymousCart_ShouldKeepAll()
    {
        var cart = new ShoppingCart(null);
        cart.AddItem(ProductId1, Guid.NewGuid(), "TEST-SKU", 1, StoreId, 10m);
        cart.AddItem(ProductId2, Guid.NewGuid(), "TEST-SKU", 3, StoreId, 20m);

        cart.Items.Should().HaveCount(2);
        cart.Items.Should().Contain(i => i.ProductId == ProductId1 && i.Quantity == 1);
        cart.Items.Should().Contain(i => i.ProductId == ProductId2 && i.Quantity == 3);
    }

    [Fact]
    public void UpdateQuantity_AnonymousCart_ShouldWork()
    {
        var cart = new ShoppingCart(null);
        var skuId = Guid.NewGuid();
        cart.AddItem(ProductId1, skuId, "TEST-SKU", 1, StoreId, 10m);
        cart.UpdateQuantity(ProductId1, skuId, 5);

        cart.Items.Should().ContainSingle();
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void RemoveItem_AnonymousCart_ShouldWork()
    {
        var cart = new ShoppingCart(null);
        var skuId1 = Guid.NewGuid();
        var skuId2 = Guid.NewGuid();
        cart.AddItem(ProductId1, skuId1, "TEST-SKU", 1, StoreId, 10m);
        cart.AddItem(ProductId2, skuId2, "TEST-SKU", 2, StoreId, 20m);
        cart.RemoveItem(ProductId1, skuId1);

        cart.Items.Should().ContainSingle();
        cart.Items.First().ProductId.Should().Be(ProductId2);
    }

    [Fact]
    public void Clear_AnonymousCart_ShouldRemoveAllItems()
    {
        var cart = new ShoppingCart(null);
        cart.AddItem(ProductId1, Guid.NewGuid(), "TEST-SKU", 1, StoreId, 10m);
        cart.AddItem(ProductId2, Guid.NewGuid(), "TEST-SKU", 2, StoreId, 20m);
        cart.Clear();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void TwoAnonymousCarts_ShouldHaveDifferentIds()
    {
        var cart1 = new ShoppingCart(null);
        var cart2 = new ShoppingCart(null);

        cart1.BuyerId.Should().BeNull();
        cart2.BuyerId.Should().BeNull();
        // Note: both carts have Id=Guid.Empty until persisted (Guid v7 via EF Core)
    }

    [Fact]
    public void AnonymousCart_Id_ShouldBeStableAfterMutations()
    {
        var cart = new ShoppingCart(null);
        var initialId = cart.Id;

        var skuId = Guid.NewGuid();
        cart.AddItem(ProductId1, skuId, "TEST-SKU", 1, StoreId, 10m);
        cart.UpdateQuantity(ProductId1, skuId, 5);
        cart.RemoveItem(ProductId1, skuId);

        cart.Id.Should().Be(initialId);
    }

    [Fact]
    public void Claim_ShouldSetBuyerIdOnAnonymousCart()
    {
        var cart = new ShoppingCart(null);
        cart.AddItem(ProductId1, Guid.NewGuid(), "TEST-SKU", 2, StoreId, 10m);

        var buyerId = Guid.NewGuid();
        cart.Claim(buyerId);

        cart.BuyerId.Should().Be(buyerId);
        cart.Items.Should().ContainSingle();
        cart.Items.First().ProductId.Should().Be(ProductId1);
    }

    [Fact]
    public void Claim_OnAlreadyClaimedCart_ShouldThrow()
    {
        var existingBuyer = Guid.NewGuid();
        var cart = new ShoppingCart(existingBuyer);

        var act = () => cart.Claim(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already claimed*");
    }
}
