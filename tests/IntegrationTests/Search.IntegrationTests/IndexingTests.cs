using FluentAssertions;
using Search.API.Models;

namespace Search.IntegrationTests;

[Collection("Search collection")]
public class IndexingTests
{
    private readonly SearchDatabaseFixture _fixture;

    public IndexingTests(SearchDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IndexProduct_CanBeRetrievedById()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var document = new ProductSearchDocument
        {
            Id = productId,
            Name = "Test Widget",
            Description = "A useful widget",
            Price = 19.99m,
            Currency = "USD",
            Sku = "WIDGET-001",
            CategoryId = Guid.NewGuid(),
            CategoryName = "Gadgets",
            Tags = ["widget", "tool"],
            ImageUrl = "http://example.com/widget.jpg",
            SellerId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _fixture.SearchService.IndexProductAsync(document);
        await _fixture.Client.Indices.RefreshAsync("marketplace-products");

        // Assert
        var response = await _fixture.Client.GetAsync<ProductSearchDocument>(
            productId.ToString(), g => g.Index("marketplace-products"));

        response.Found.Should().BeTrue();
        var retrieved = response.Source;
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Widget");
        retrieved.Price.Should().Be(19.99m);
        retrieved.Sku.Should().Be("WIDGET-001");
    }

    [Fact]
    public async Task UpdateProduct_VerifyNewFields()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var document = new ProductSearchDocument
        {
            Id = productId,
            Name = "Original Name",
            Description = "Original desc",
            Price = 10m,
            Currency = "USD",
            Sku = "UPD-001",
            CategoryId = Guid.NewGuid(),
            CategoryName = "Original Cat",
            Tags = ["old"],
            SellerId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fixture.SearchService.IndexProductAsync(document);
        await _fixture.Client.Indices.RefreshAsync("marketplace-products");

        // Act - update the document
        var updated = new ProductSearchDocument
        {
            Id = productId,
            Name = "Updated Name",
            Description = "Updated desc",
            Price = 25m,
            Currency = "EUR",
            Sku = "UPD-001",
            CategoryId = Guid.NewGuid(),
            CategoryName = "Updated Cat",
            Tags = ["new"],
            SellerId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = document.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await _fixture.SearchService.UpdateProductAsync(updated);
        await _fixture.Client.Indices.RefreshAsync("marketplace-products");

        // Assert
        var response = await _fixture.Client.GetAsync<ProductSearchDocument>(
            productId.ToString(), g => g.Index("marketplace-products"));

        response.Found.Should().BeTrue();
        var retrieved = response.Source!;
        retrieved.Name.Should().Be("Updated Name");
        retrieved.Price.Should().Be(25m);
        retrieved.Currency.Should().Be("EUR");
    }
}
