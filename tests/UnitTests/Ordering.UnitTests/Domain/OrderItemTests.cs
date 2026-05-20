using FluentAssertions;
using Ordering.Domain.Aggregates.Entities;

namespace Ordering.UnitTests.Domain;

public class OrderItemTests
{
    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Constructor_WithValidData_CreatesItem()
    {
        var productId = Guid.NewGuid();
        var item = new OrderItem(productId, "Product", 10.50m, 3, TestStoreId);

        item.ProductId.Should().Be(productId);
        item.ProductName.Should().Be("Product");
        item.UnitPrice.Should().Be(10.50m);
        item.Quantity.Should().Be(3);
        item.StoreId.Should().Be(TestStoreId);
        item.TotalPrice.Should().Be(31.50m);
    }

    [Fact]
    public void Constructor_WithEmptyProductId_Throws()
    {
        var act = () => new OrderItem(Guid.Empty, "Product", 10m, 1, TestStoreId);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyProductName_Throws(string name)
    {
        var act = () => new OrderItem(Guid.NewGuid(), name, 10m, 1, TestStoreId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNegativePrice_Throws()
    {
        var act = () => new OrderItem(Guid.NewGuid(), "Product", -1m, 1, TestStoreId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithZeroPrice_Succeeds()
    {
        var item = new OrderItem(Guid.NewGuid(), "Product", 0m, 1, TestStoreId);
        item.UnitPrice.Should().Be(0m);
    }

    [Fact]
    public void Constructor_WithZeroQuantity_Throws()
    {
        var act = () => new OrderItem(Guid.NewGuid(), "Product", 10m, 0, TestStoreId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TotalPrice_MultipliesPriceByQuantity()
    {
        var item = new OrderItem(Guid.NewGuid(), "Product", 7.50m, 4, TestStoreId);
        item.TotalPrice.Should().Be(30.00m);
    }

    [Fact]
    public void Constructor_WithStoreId_SetsStoreId()
    {
        var item = new OrderItem(Guid.NewGuid(), "Product", 10m, 1, TestStoreId);
        item.StoreId.Should().Be(TestStoreId);
    }

    [Fact]
    public void Constructor_WithEmptyStoreId_Throws()
    {
        var act = () => new OrderItem(Guid.NewGuid(), "Product", 10m, 1, Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
