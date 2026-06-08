using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Catalog.UnitTests.Domain.Builders;
using Catalog.Domain.Enums;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Catalog.UnitTests.Domain;

public class SkuTests
{
    [Fact]
    public void UpdateAttributes_ModifiesDictionaries()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Shirts")
            .WithAttributeDefinition("size", "Size", AttributeTarget.Sku, AttributeType.Select)
            .WithProduct("T-Shirt")
            .WithVariantAxes("size")
            .WithSku("TSHIRT-M", 19.99m, new Dictionary<string, string> { ["size"] = "M" });

        var sku = builder.GetSku("TSHIRT-M");

        // Act
        sku.UpdateAttributes(
            typedAttributes: new Dictionary<string, string> { ["size"] = "L" },
            flexibleAttributes: new Dictionary<string, string> { ["material"] = "Cotton" }
        );

        // Assert
        sku.TypedAttributes.Should().ContainKey("size").WhoseValue.Should().Be("L");
        sku.FlexibleAttributes.Should().ContainKey("material").WhoseValue.Should().Be("Cotton");
    }

    [Fact]
    public void ChangePrice_UpdatesPriceAndUpdatedAt()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Shirts")
            .WithProduct("T-Shirt")
            .WithSku("TSHIRT-M", 19.99m, new Dictionary<string, string>());

        var sku = builder.GetSku("TSHIRT-M");
        var newPrice = Money.Create(24.99m, "USD");

        // Act
        sku.ChangePrice(newPrice);

        // Assert
        sku.Price.Amount.Should().Be(24.99m);
        sku.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddOrUpdateAttributeValue_AddsNewValue()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Shirts")
            .WithProduct("T-Shirt")
            .WithSku("TSHIRT-M", 19.99m, new Dictionary<string, string>());

        var sku = builder.GetSku("TSHIRT-M");
        var attrId = Guid.NewGuid();

        // Act
        sku.AddOrUpdateAttributeValue(attrId, "Cotton");

        // Assert
        sku.AttributeValues.Should().ContainSingle();
        sku.AttributeValues.First().Value.Should().Be("Cotton");
    }

    [Fact]
    public void AddOrUpdateAttributeValue_UpdatesExistingValue()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Shirts")
            .WithProduct("T-Shirt")
            .WithSku("TSHIRT-M", 19.99m, new Dictionary<string, string>());

        var sku = builder.GetSku("TSHIRT-M");
        var attrId = Guid.NewGuid();
        sku.AddOrUpdateAttributeValue(attrId, "Cotton");

        // Act
        sku.AddOrUpdateAttributeValue(attrId, "Polyester");

        // Assert
        sku.AttributeValues.Should().ContainSingle();
        sku.AttributeValues.First().Value.Should().Be("Polyester");
    }

    [Fact]
    public void RemoveAttributeValue_RemovesExistingValue()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Shirts")
            .WithProduct("T-Shirt")
            .WithSku("TSHIRT-M", 19.99m, new Dictionary<string, string>());

        var sku = builder.GetSku("TSHIRT-M");
        var attrId = Guid.NewGuid();
        sku.AddOrUpdateAttributeValue(attrId, "Cotton");

        // Act
        sku.RemoveAttributeValue(attrId);

        // Assert
        sku.AttributeValues.Should().BeEmpty();
    }
}
