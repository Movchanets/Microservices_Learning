using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Events;
using Catalog.UnitTests.Domain.Builders;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Catalog.UnitTests.Domain;

public class ProductAggregateTests
{
    [Fact]
    public void AddVariantAxis_DuplicateAttribute_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Test Category")
            .WithAttributeDefinition("color", "Color", AttributeTarget.Sku, AttributeType.Select)
            .WithProduct("Test Product")
            .WithVariantAxes("color");

        var product = builder.BuildProduct();
        var category = builder.BuildCategory();
        var colorAttrId = category.AttributeDefinitions.First(a => a.Key == "color").Id;

        // Act
        Action action = () => product.AddVariantAxis(colorAttrId, 1);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*already exists*");
    }

    [Fact]
    public void AddSku_WithDuplicateVariantSignature_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Phones")
            .WithAttributeDefinition("color", "Color", AttributeTarget.Sku, AttributeType.Select)
            .WithAttributeDefinition("storage", "Storage", AttributeTarget.Sku, AttributeType.Select)
            .WithProduct("Phone Model X")
            .WithVariantAxes("color", "storage")
            .WithSku("PHX-BLK-128", 999m, new Dictionary<string, string> 
            { 
                ["color"] = "Black", 
                ["storage"] = "128GB" 
            });

        var product = builder.BuildProduct();

        // Act
        Action action = () => builder.WithSku("PHX-BLK-128-DUP", 999m, new Dictionary<string, string>
        {
            ["color"] = "Black",
            ["storage"] = "128GB"
        });

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*already exists on product*");
    }

    [Fact]
    public void AddSku_WithDifferentVariantSignature_Succeeds()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Phones")
            .WithAttributeDefinition("color", "Color", AttributeTarget.Sku, AttributeType.Select)
            .WithAttributeDefinition("storage", "Storage", AttributeTarget.Sku, AttributeType.Select)
            .WithProduct("Phone Model X")
            .WithVariantAxes("color", "storage")
            .WithSku("PHX-BLK-128", 999m, new Dictionary<string, string> 
            { 
                ["color"] = "Black", 
                ["storage"] = "128GB" 
            });

        // Act
        builder.WithSku("PHX-BLK-256", 1099m, new Dictionary<string, string>
        {
            ["color"] = "Black",
            ["storage"] = "256GB"
        });
        
        var product = builder.BuildProduct();

        // Assert
        product.Skus.Should().HaveCount(2);
        product.Skus.Should().Contain(s => s.SkuCode == "PHX-BLK-256");
    }

    [Fact]
    public void SetVariantAxes_ReplacesExistingAxes()
    {
        // Arrange
        var builder = new CatalogDataBuilder()
            .WithCategory("Phones")
            .WithAttributeDefinition("color", "Color", AttributeTarget.Sku, AttributeType.Select)
            .WithAttributeDefinition("storage", "Storage", AttributeTarget.Sku, AttributeType.Select)
            .WithProduct("Phone Model X")
            .WithVariantAxes("color");

        var product = builder.BuildProduct();
        var category = builder.BuildCategory();
        var storageAttrId = category.AttributeDefinitions.First(a => a.Key == "storage").Id;

        // Act
        product.SetVariantAxes(new[] { storageAttrId });

        // Assert
        product.VariantAxes.Should().ContainSingle();
        product.VariantAxes.First().AttributeDefinitionId.Should().Be(storageAttrId);
    }

    [Fact]
    public void Create_AssignsNonEmptyId_AndEventCarriesIt()
    {
        // Arrange & Act
        var product = Product.Create(
            "Test Product", "Description", Guid.NewGuid(), Guid.NewGuid());

        // Assert — Id must be assigned before domain events are raised
        product.Id.Should().NotBe(Guid.Empty);

        var domainEvent = product.DomainEvents
            .OfType<ProductCreatedDomainEvent>()
            .Single();

        domainEvent.ProductId.Should().Be(product.Id);
    }
}
