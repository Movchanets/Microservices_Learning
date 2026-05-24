using Catalog.Domain.Aggregates;
using Catalog.Domain.Events;
using FluentAssertions;
using System;
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
        var price = 10m;
        var currency = "USD";
        var sku = "TEST-SKU-1";
        var categoryId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var product = Product.Create(name, description, price, currency, sku, categoryId, storeId);

        // Assert
        product.Should().NotBeNull();
        product.DomainEvents.Should().ContainSingle();
        var domainEvent = product.DomainEvents.First();
        domainEvent.Should().BeOfType<ProductCreatedDomainEvent>();

        var productCreatedEvent = (ProductCreatedDomainEvent)domainEvent;
        productCreatedEvent.ProductId.Should().Be(product.Id);
        productCreatedEvent.Name.Should().Be(name);
        productCreatedEvent.Sku.Should().Be(sku);
    }

    [Fact]
    public void ChangePrice_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var product = Product.Create("Test", "Test", 10m, "USD", "SKU1", Guid.NewGuid(), Guid.NewGuid());
        product.ClearDomainEvents();

        // Act
        Action action = () => product.ChangePrice(-5m, "USD");

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Amount cannot be negative*");
    }

    [Fact]
    public void ChangePrice_ValidAmount_GeneratesProductPriceChangedDomainEvent()
    {
        // Arrange
        var product = Product.Create("Test", "Test", 10m, "USD", "SKU1", Guid.NewGuid(), Guid.NewGuid());
        product.ClearDomainEvents();
        var newPrice = 15m;
        var currency = "USD";

        // Act
        product.ChangePrice(newPrice, currency);

        // Assert
        product.DomainEvents.Should().ContainSingle();
        var domainEvent = product.DomainEvents.First();
        domainEvent.Should().BeOfType<ProductPriceChangedDomainEvent>();

        var priceChangedEvent = (ProductPriceChangedDomainEvent)domainEvent;
        priceChangedEvent.ProductId.Should().Be(product.Id);
        priceChangedEvent.OldPrice.Should().Be(10m);
        priceChangedEvent.NewPrice.Should().Be(newPrice);
        priceChangedEvent.Currency.Should().Be(currency);
    }
}
