using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Models;
using Search.API.Services;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Catalog product lifecycle events
/// are correctly consumed by the Search microservice (Elasticsearch).
///
/// Ensures the message contract between Catalog publisher and
/// Search consumer is stable and compatible.
/// </summary>
public class CatalogToSearchContractTests
{
    private readonly Mock<ISearchService> _searchServiceMock;
    private readonly ProductCreatedConsumer _createdConsumer;
    private readonly ProductUpdatedConsumer _updatedConsumer;
    private readonly ProductDeletedConsumer _deletedConsumer;

    public CatalogToSearchContractTests()
    {
        _searchServiceMock = new Mock<ISearchService>();
        _createdConsumer = new ProductCreatedConsumer(
            _searchServiceMock.Object, Mock.Of<ILogger<ProductCreatedConsumer>>());
        _updatedConsumer = new ProductUpdatedConsumer(
            _searchServiceMock.Object, Mock.Of<ILogger<ProductUpdatedConsumer>>());
        _deletedConsumer = new ProductDeletedConsumer(
            _searchServiceMock.Object, Mock.Of<ILogger<ProductDeletedConsumer>>());
    }

    [Fact]
    public async Task ProductCreatedEvent_Contract_ShouldIndexProductInSearch()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var @event = new ProductCreatedEvent(
            ProductId: productId,
            Name: "Searchable Product",
            Description: "A searchable product",
            Price: 99.99m,
            Currency: "USD",
            Sku: "SKU-SEARCH-001",
            CategoryId: categoryId,
            CategoryName: "Electronics",
            Tags: ["searchable", "electronics"],
            ImageUrl: "https://example.com/img.jpg",
            StoreId: storeId,
            CreatedAt: createdAt,
            Brand: "SearchBrand",
            Attributes: new Dictionary<string, string> { ["weight"] = "1.5kg" });

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        ProductSearchDocument? capturedDoc = null;
        _searchServiceMock
            .Setup(x => x.IndexProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ProductSearchDocument, CancellationToken>((doc, _) => capturedDoc = doc)
            .Returns(Task.CompletedTask);

        // Act
        await _createdConsumer.Consume(consumeContext.Object);

        // Assert - verify the contract mapping
        capturedDoc.Should().NotBeNull();
        capturedDoc!.Id.Should().Be(productId);
        capturedDoc.Name.Should().Be("Searchable Product");
        capturedDoc.Description.Should().Be("A searchable product");
        capturedDoc.Price.Should().Be(99.99m);
        capturedDoc.Currency.Should().Be("USD");
        capturedDoc.Sku.Should().Be("SKU-SEARCH-001");
        capturedDoc.CategoryId.Should().Be(categoryId);
        capturedDoc.CategoryName.Should().Be("Electronics");
        capturedDoc.Tags.Should().Contain("searchable");
        capturedDoc.ImageUrl.Should().Be("https://example.com/img.jpg");
        capturedDoc.StoreId.Should().Be(storeId);
        capturedDoc.IsActive.Should().BeTrue();
        capturedDoc.CreatedAt.Should().Be(createdAt);
        capturedDoc.Brand.Should().Be("SearchBrand");
        capturedDoc.Attributes.Should().ContainKey("weight");
    }

    [Fact]
    public async Task ProductUpdatedEvent_Contract_ShouldUpdateProductInSearch()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;

        var @event = new ProductUpdatedEvent(
            ProductId: productId,
            Name: "Updated Search Product",
            Description: "Updated description",
            Price: 149.99m,
            Currency: "EUR",
            Sku: "SKU-SEARCH-UPD",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Updated Category",
            Tags: ["updated"],
            ImageUrl: null,
            IsActive: true,
            UpdatedAt: updatedAt,
            Brand: "UpdatedBrand",
            Attributes: new Dictionary<string, string> { ["size"] = "XL" });

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        ProductSearchDocument? capturedDoc = null;
        _searchServiceMock
            .Setup(x => x.UpdateProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ProductSearchDocument, CancellationToken>((doc, _) => capturedDoc = doc)
            .Returns(Task.CompletedTask);

        // Act
        await _updatedConsumer.Consume(consumeContext.Object);

        // Assert
        capturedDoc.Should().NotBeNull();
        capturedDoc!.Id.Should().Be(productId);
        capturedDoc.Name.Should().Be("Updated Search Product");
        capturedDoc.Price.Should().Be(149.99m);
        capturedDoc.Currency.Should().Be("EUR");
        capturedDoc.IsActive.Should().BeTrue();
        capturedDoc.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public async Task ProductUpdatedEvent_Contract_ShouldHandleInactiveProducts()
    {
        // Arrange
        var @event = new ProductUpdatedEvent(
            ProductId: Guid.NewGuid(),
            Name: "Inactive Product",
            Description: "Desc",
            Price: 10m,
            Currency: "USD",
            Sku: "SKU-INACTIVE",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Cat",
            Tags: [],
            ImageUrl: null,
            IsActive: false,
            UpdatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        ProductSearchDocument? capturedDoc = null;
        _searchServiceMock
            .Setup(x => x.UpdateProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ProductSearchDocument, CancellationToken>((doc, _) => capturedDoc = doc)
            .Returns(Task.CompletedTask);

        // Act
        await _updatedConsumer.Consume(consumeContext.Object);

        // Assert
        capturedDoc.Should().NotBeNull();
        capturedDoc!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ProductDeletedEvent_Contract_ShouldRemoveProductFromSearch()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new ProductDeletedEvent(productId, DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        Guid? capturedId = null;
        _searchServiceMock
            .Setup(x => x.DeleteProductAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((id, _) => capturedId = id)
            .Returns(Task.CompletedTask);

        // Act
        await _deletedConsumer.Consume(consumeContext.Object);

        // Assert
        capturedId.Should().Be(productId);
    }
}
