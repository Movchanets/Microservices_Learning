using Cart.Domain.Aggregates;
using FluentAssertions;

namespace Cart.UnitTests.Domain;

public class ShoppingCartTests
{
    private static readonly Guid StoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProductId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void AddItem_WhenNewProduct_ShouldAddNewItem()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        cart.AddItem(ProductId1, Guid.NewGuid(), "TEST-SKU", 2, StoreId, 10m);

        cart.Items.Should().ContainSingle();
        cart.Items.First().ProductId.Should().Be(ProductId1);
        cart.Items.First().Quantity.Should().Be(2);
        cart.Items.First().StoreId.Should().Be(StoreId);
    }

    [Fact]
    public void AddItem_WhenExistingProduct_ShouldIncrementQuantity()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        var skuId = Guid.NewGuid();
        cart.AddItem(ProductId1, skuId, "TEST-SKU", 2, StoreId, 10m);
        cart.AddItem(ProductId1, skuId, "TEST-SKU", 3, StoreId, 10m);

        cart.Items.Should().ContainSingle();
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateQuantity_WhenGreaterThanZero_ShouldUpdateQuantity()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        var skuId = Guid.NewGuid();
        cart.AddItem(ProductId1, skuId, "TEST-SKU", 2, StoreId, 10m);

        cart.UpdateQuantity(ProductId1, skuId, 5);

        cart.Items.Should().ContainSingle();
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateQuantity_WhenZeroOrLess_ShouldRemoveItem()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        var skuId1 = Guid.NewGuid();
        var skuId2 = Guid.NewGuid();
        cart.AddItem(ProductId1, skuId1, "TEST-SKU", 2, StoreId, 10m);
        cart.AddItem(ProductId2, skuId2, "TEST-SKU", 1, StoreId, 20m);

        cart.UpdateQuantity(ProductId1, skuId1, 0);
        cart.UpdateQuantity(ProductId2, skuId2, -1);

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_ShouldRemoveTheItem()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        var skuId1 = Guid.NewGuid();
        var skuId2 = Guid.NewGuid();
        cart.AddItem(ProductId1, skuId1, "TEST-SKU", 2, StoreId, 10m);
        cart.AddItem(ProductId2, skuId2, "TEST-SKU", 1, StoreId, 20m);

        cart.RemoveItem(ProductId1, skuId1);

        cart.Items.Should().ContainSingle();
        cart.Items.First().ProductId.Should().Be(ProductId2);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        var cart = new ShoppingCart(Guid.NewGuid());
        cart.AddItem(ProductId1, Guid.NewGuid(), "TEST-SKU", 2, StoreId, 10m);
        cart.AddItem(ProductId2, Guid.NewGuid(), "TEST-SKU", 1, StoreId, 20m);

        cart.Clear();

        cart.Items.Should().BeEmpty();
    }
}
