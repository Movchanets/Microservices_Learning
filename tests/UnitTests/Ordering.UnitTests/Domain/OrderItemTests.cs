// OrderItem entity unit tests.
// Validates constructor guard clauses (SKU, ProductName, UnitPrice, Quantity),
// the computed TotalPrice property, and edge cases like zero-priced items
// (allowed for saga-created orders where price is enriched later).

using FluentAssertions;
using Ordering.Domain.Aggregates.Entities;

namespace Ordering.UnitTests.Domain;

public class OrderItemTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesItem()
    {
        var item = new OrderItem("SKU-1", "Product", 10.50m, 3);

        item.Sku.Should().Be("SKU-1");
        item.ProductName.Should().Be("Product");
        item.UnitPrice.Should().Be(10.50m);
        item.Quantity.Should().Be(3);
        item.TotalPrice.Should().Be(31.50m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptySku_Throws(string sku)
    {
        var act = () => new OrderItem(sku, "Product", 10m, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyProductName_Throws(string name)
    {
        var act = () => new OrderItem("SKU", name, 10m, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNegativePrice_Throws()
    {
        var act = () => new OrderItem("SKU", "Product", -1m, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithZeroPrice_Succeeds()
    {
        var item = new OrderItem("SKU", "Product", 0m, 1);

        item.UnitPrice.Should().Be(0m);
    }

    [Fact]
    public void Constructor_WithZeroQuantity_Throws()
    {
        var act = () => new OrderItem("SKU", "Product", 10m, 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TotalPrice_MultipliesPriceByQuantity()
    {
        var item = new OrderItem("SKU", "Product", 7.50m, 4);

        item.TotalPrice.Should().Be(30.00m);
    }
}
