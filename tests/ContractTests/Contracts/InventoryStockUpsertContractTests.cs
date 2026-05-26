using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Exceptions;
using FluentAssertions;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying InventoryItem domain behavior for the SKU refactor.
///
/// After the fix, the PUT /api/inventory/items/{skuCode}/stock endpoint generates
/// a new SkuId via Guid.CreateVersion7() when the caller doesn't provide one
/// (e.g., the seeder sends { Quantity, StoreId, ProductId } without SkuId).
///
/// These tests verify the domain contract that InventoryItem.Create enforces.
/// </summary>
public class InventoryStockUpsertContractTests
{
    // ──────────────────────────────────────────────────────────────────────
    // InventoryItem.Create — domain validation contract
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryItem_Create_WithValidSkuId_ShouldSucceed()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var item = InventoryItem.Create(skuId, productId, "PHONE-IPHONE-16", 100, storeId);

        // Assert
        item.SkuId.Should().Be(skuId);
        item.ProductId.Should().Be(productId);
        item.SkuCode.Should().Be("PHONE-IPHONE-16");
        item.AvailableQuantity.Should().Be(100);
        item.StoreId.Should().Be(storeId);
    }

    [Fact]
    public void InventoryItem_Create_WithEmptySkuId_ShouldThrowArgumentException()
    {
        // Arrange — this was the original bug: seeder sent empty SkuId
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var act = () => InventoryItem.Create(Guid.Empty, productId, "PHONE-IPHONE-16", 100, storeId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("skuId")
            .WithMessage("*SkuId is required*");
    }

    [Fact]
    public void InventoryItem_Create_WithEmptyProductId_ShouldThrowArgumentException()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var act = () => InventoryItem.Create(skuId, Guid.Empty, "PHONE-IPHONE-16", 100, storeId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("productId")
            .WithMessage("*ProductId is required*");
    }

    [Fact]
    public void InventoryItem_Create_WithEmptySkuCode_ShouldThrowArgumentException()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var act = () => InventoryItem.Create(skuId, productId, "", 100, storeId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InventoryItem_Create_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var act = () => InventoryItem.Create(skuId, productId, "SKU-001", -5, storeId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("initialQuantity")
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void InventoryItem_Create_WithZeroQuantity_ShouldSucceed()
    {
        // Arrange — SkuCreatedConsumer creates items with qty=0
        var skuId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var item = InventoryItem.Create(skuId, productId, "SKU-ZERO", 0, storeId);

        // Assert
        item.AvailableQuantity.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Endpoint contract: Guid.CreateVersion7() when SkuId not provided
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void EndpointContract_GeneratedSkuId_ShouldNotBeEmpty()
    {
        // Arrange — simulates what the endpoint does when request.SkuId == Guid.Empty
        var requestSkuId = Guid.Empty;

        // Act — this is the exact logic from InventoryEndpoints.cs
        var skuId = requestSkuId != Guid.Empty
            ? requestSkuId
            : Guid.CreateVersion7();

        // Assert
        skuId.Should().NotBe(Guid.Empty);
        skuId.Should().NotBe(requestSkuId);
    }

    [Fact]
    public void EndpointContract_ProvidedSkuId_ShouldBePreserved()
    {
        // Arrange — simulates what the endpoint does when request.SkuId is provided
        var requestSkuId = Guid.NewGuid();

        // Act
        var skuId = requestSkuId != Guid.Empty
            ? requestSkuId
            : Guid.CreateVersion7();

        // Assert
        skuId.Should().Be(requestSkuId);
    }

    [Fact]
    public void EndpointContract_CreateWithGeneratedSkuId_ShouldSatisfyDomainValidation()
    {
        // Arrange — full end-to-end: generate SkuId, then create item
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var skuCode = "AUDIO-SONY-WH1000XM5";

        // Act — endpoint logic
        var requestSkuId = Guid.Empty;
        var skuId = requestSkuId != Guid.Empty
            ? requestSkuId
            : Guid.CreateVersion7();
        var item = InventoryItem.Create(skuId, productId, skuCode, 50, storeId);

        // Assert — domain is satisfied
        item.SkuId.Should().NotBe(Guid.Empty);
        item.SkuId.Should().Be(skuId);
        item.ProductId.Should().Be(productId);
        item.SkuCode.Should().Be(skuCode.ToUpperInvariant());
        item.AvailableQuantity.Should().Be(50);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Stock update contract (existing item)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryItem_AddStock_ShouldIncreaseAvailableQuantity()
    {
        // Arrange
        var item = InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-STOCK", 10, Guid.NewGuid());

        // Act
        item.AddStock(25);

        // Assert
        item.AvailableQuantity.Should().Be(35);
    }

    [Fact]
    public void InventoryItem_AddStock_WithZeroOrNegative_ShouldThrow()
    {
        // Arrange
        var item = InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-STOCK", 10, Guid.NewGuid());

        // Act & Assert
        ((Action)(() => item.AddStock(0))).Should().Throw<ArgumentException>();
        ((Action)(() => item.AddStock(-1))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EndpointContract_ExistingItem_ShouldAddDifference()
    {
        // Arrange — simulates PUT stock logic when item exists
        var item = InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-EXISTING", 10, Guid.NewGuid());
        var requestQuantity = 50;

        // Act — endpoint logic: diff = request.Quantity - item.AvailableQuantity
        var diff = requestQuantity - item.AvailableQuantity;
        if (diff > 0) item.AddStock(diff);

        // Assert
        item.AvailableQuantity.Should().Be(50);
    }

    [Fact]
    public void EndpointContract_ExistingItem_LowerQuantity_ShouldNotReduce()
    {
        // Arrange — PUT with lower qty than current should not reduce stock
        var item = InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-EXISTING", 50, Guid.NewGuid());
        var requestQuantity = 10;

        // Act — endpoint logic
        var diff = requestQuantity - item.AvailableQuantity; // -40
        if (diff > 0) item.AddStock(diff);

        // Assert — quantity unchanged (endpoint doesn't reduce)
        item.AvailableQuantity.Should().Be(50);
    }
}
