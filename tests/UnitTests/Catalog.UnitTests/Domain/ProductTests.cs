using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Events;
using Catalog.Domain.ValueObjects;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_ValidInputs_GeneratesProductCreatedDomainEvent()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var categoryId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var product = Product.Create(name, description, categoryId, storeId);

        // Assert
        product.Should().NotBeNull();
        product.DomainEvents.Should().ContainSingle();
        var domainEvent = product.DomainEvents.First();
        domainEvent.Should().BeOfType<ProductCreatedDomainEvent>();

        var productCreatedEvent = (ProductCreatedDomainEvent)domainEvent;
        productCreatedEvent.ProductId.Should().Be(product.Id);
        productCreatedEvent.Name.Should().Be(name);
        productCreatedEvent.Description.Should().Be(description);
        productCreatedEvent.CategoryId.Should().Be(categoryId);
        productCreatedEvent.StoreId.Should().Be(storeId);
    }

    [Fact]
    public void AddSku_ValidInputs_GeneratesSkuCreatedDomainEvent()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", Guid.NewGuid(), Guid.NewGuid());
        product.ClearDomainEvents();
        var price = Money.Create(29.99m, "USD");

        // Act
        var sku = product.AddSku("SKU-001", price, new Dictionary<string, string> { { "color", "red" } });

        // Assert
        sku.Should().NotBeNull();
        sku.SkuCode.Should().Be("SKU-001");
        sku.Price.Amount.Should().Be(29.99m);
        sku.Price.Currency.Should().Be("USD");
        product.Skus.Should().ContainSingle();
        product.DomainEvents.Should().ContainSingle();
        product.DomainEvents.First().Should().BeOfType<SkuCreatedDomainEvent>();
    }

    [Fact]
    public void AddSku_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", Guid.NewGuid(), Guid.NewGuid());
        var price = Money.Create(29.99m, "USD");
        product.AddSku("SKU-001", price, new Dictionary<string, string>());

        // Act
        Action action = () => product.AddSku("SKU-001", price, new Dictionary<string, string>());

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveSku_ValidSkuId_MarksSkuAsDeletedAndGeneratesEvent()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", Guid.NewGuid(), Guid.NewGuid());
        var price = Money.Create(10m, "USD");
        var sku = product.AddSku("SKU-001", price, new Dictionary<string, string>());
        product.ClearDomainEvents();

        // Act
        product.RemoveSku(sku.Id);

        // Assert
        product.DomainEvents.Should().ContainSingle();
        product.DomainEvents.First().Should().BeOfType<SkuDeletedDomainEvent>();
        sku.Status.Should().Be(SkuStatus.Deleted);
    }

    [Fact]
    public void RemoveSku_NonExistentSkuId_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", Guid.NewGuid(), Guid.NewGuid());

        // Act
        Action action = () => product.RemoveSku(Guid.NewGuid());

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void GetSku_ValidSkuId_ReturnsSku()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", Guid.NewGuid(), Guid.NewGuid());
        var price = Money.Create(10m, "USD");
        var sku = product.AddSku("SKU-001", price, new Dictionary<string, string>());

        // Act
        var result = product.GetSku(sku.Id);

        // Assert
        result.Should().Be(sku);
    }

    [Fact]
    public void GetSku_NonExistentSkuId_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", Guid.NewGuid(), Guid.NewGuid());

        // Act
        Action action = () => product.GetSku(Guid.NewGuid());

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }
}
