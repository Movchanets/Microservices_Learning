using FluentAssertions;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Events;
using Inventory.Domain.Exceptions;

namespace Inventory.UnitTests.Domain;

public class InventoryItemTests
{
    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestSkuId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_WithValidData_InitializesCorrectly()
    {
        // Arrange
        var sku = "  test-sku  ";
        var initialQuantity = 10;

        // Act
        var item = InventoryItem.Create(TestSkuId, TestProductId, sku, initialQuantity, TestStoreId);

        // Assert
        item.SkuCode.Should().Be("TEST-SKU");
        item.AvailableQuantity.Should().Be(10);
        item.StoreId.Should().Be(TestStoreId);
        item.ProductId.Should().Be(TestProductId);
    }

    [Fact]
    public void Reserve_WithAvailableStock_DeductsQuantityAndGeneratesEvent()
    {
        // Arrange
        var item = InventoryItem.Create(TestSkuId, TestProductId, "TEST-SKU", 10, TestStoreId);
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
        var item = InventoryItem.Create(TestSkuId, TestProductId, "TEST-SKU", 5, TestStoreId);
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
        var item = InventoryItem.Create(TestSkuId, TestProductId, "TEST-SKU", 5, TestStoreId);
        item.Reserve(3);  // Available=2, Reserved=3
        item.ClearDomainEvents();

        // Act
        item.Release(3);

        // Assert
        item.AvailableQuantity.Should().Be(5);

        var domainEvents = item.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReleasedDomainEvent>()
            .Which.Quantity.Should().Be(3);
    }
}
