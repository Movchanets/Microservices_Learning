using FluentAssertions;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Events;
using Inventory.Domain.Exceptions;

namespace Inventory.UnitTests.Domain;

public class InventoryItemTests
{
    [Fact]
    public void Create_WithValidData_InitializesCorrectly()
    {
        // Arrange
        var sku = "  test-sku  ";
        var initialQuantity = 10;

        // Act
        var item = InventoryItem.Create(sku, initialQuantity);

        // Assert
        item.Sku.Should().Be("TEST-SKU");
        item.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public void Reserve_WithAvailableStock_DeductsQuantityAndGeneratesEvent()
    {
        // Arrange
        var item = InventoryItem.Create("TEST-SKU", 10);
        item.ClearDomainEvents();

        // Act
        item.Reserve(4);

        // Assert
        item.AvailableQuantity.Should().Be(6);

        var domainEvents = item.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReservedDomainEvent>()
            .Which.Quantity.Should().Be(4);
    }

    [Fact]
    public void Reserve_WhenQuantityExceedsAvailable_ThrowsOutOfStockException()
    {
        // Arrange
        var item = InventoryItem.Create("TEST-SKU", 5);
        item.ClearDomainEvents();

        // Act
        var act = () => item.Reserve(10);

        // Assert
        act.Should().Throw<OutOfStockException>();
        item.AvailableQuantity.Should().Be(5); // unchanged
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Release_AddsQuantityBackAndGeneratesEvent()
    {
        // Arrange
        var item = InventoryItem.Create("TEST-SKU", 5);
        item.ClearDomainEvents();

        // Act
        item.Release(3);

        // Assert
        item.AvailableQuantity.Should().Be(8);

        var domainEvents = item.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReleasedDomainEvent>()
            .Which.Quantity.Should().Be(3);
    }
}
