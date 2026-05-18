using Cart.Domain.Aggregates;
using FluentAssertions;

namespace Cart.UnitTests.Domain;

public class ShoppingCartTests
{
    [Fact]
    public void AddItem_WhenNewSku_ShouldAddNewItem()
    {
        // Arrange
        var cart = new ShoppingCart("buyer-1");

        // Act
        cart.AddItem("sku-1", 2);

        // Assert
        cart.Items.Should().ContainSingle();
        cart.Items.First().Sku.Should().Be("sku-1");
        cart.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_WithShopId_ShouldPropagateShopId()
    {
        // Arrange
        var cart = new ShoppingCart("buyer-1");

        // Act
        cart.AddItem("sku-1", 2, 10m, "seller-42");

        // Assert
        cart.Items.Should().ContainSingle();
        cart.Items.First().ShopId.Should().Be("seller-42");
    }

    [Fact]
    public void AddItem_WithoutShopId_ShouldHaveNullShopId()
    {
        // Arrange
        var cart = new ShoppingCart("buyer-1");

        // Act
        cart.AddItem("sku-1", 2, 10m);

        // Assert
        cart.Items.First().ShopId.Should().BeNull();
    }

    [Fact]
    public void AddItem_WhenExistingSku_ShouldIncrementQuantity()
    {
        // Arrange
        var cart = new ShoppingCart("buyer-1");
        cart.AddItem("sku-1", 2);

        // Act
        cart.AddItem("sku-1", 3);

        // Assert
        cart.Items.Should().ContainSingle();
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateQuantity_WhenGreaterThanZero_ShouldUpdateQuantity()
    {
        // Arrange
        var cart = new ShoppingCart("buyer-1");
        cart.AddItem("sku-1", 2);

        // Act
        cart.UpdateQuantity("sku-1", 5);

        // Assert
        cart.Items.Should().ContainSingle();
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateQuantity_WhenZeroOrLess_ShouldRemoveItem()
    {
        // Arrange
        var cart = new ShoppingCart("buyer-1");
        cart.AddItem("sku-1", 2);
        cart.AddItem("sku-2", 1);

        // Act
        cart.UpdateQuantity("sku-1", 0);
        cart.UpdateQuantity("sku-2", -1);

        // Assert
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var cart = new ShoppingCart("buyer-1");
        cart.AddItem("sku-1", 2);
        cart.AddItem("sku-2", 1);

        // Act
        cart.Clear();

        // Assert
        cart.Items.Should().BeEmpty();
    }
}
