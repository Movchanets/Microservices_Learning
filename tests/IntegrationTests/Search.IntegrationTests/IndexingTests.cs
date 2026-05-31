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
            MinPrice = 19.99m,
            MaxPrice = 19.99m,
            Currency = "USD",
            SkuCount = 1,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Gadgets",
            Tags = ["widget", "tool"],
            ImageUrl = "http://example.com/widget.jpg",
            StoreId = Guid.NewGuid(),
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
        retrieved.MinPrice.Should().Be(19.99m);
        retrieved.MaxPrice.Should().Be(19.99m);
        retrieved.SkuCount.Should().Be(1);
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
            MinPrice = 10m,
            MaxPrice = 10m,
            Currency = "USD",
            SkuCount = 1,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Original Cat",
            Tags = ["old"],
            StoreId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fixture.SearchService.IndexProductAsync(document);
        await _fixture.Client.Indices.RefreshAsync("marketplace-products");

        // Act - update metadata via the new request type
        var request = new UpdateProductMetadataRequest(
            ProductId: productId,
            Name: "Updated Name",
            Description: "Updated desc",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Updated Cat",
            Tags: ["new"],
            ImageUrl: null,
            StoreId: Guid.NewGuid(),
            IsActive: true,
            UpdatedAt: DateTime.UtcNow,
            Brand: null,
            Attributes: null);

        await _fixture.SearchService.UpdateProductMetadataAsync(request);
        await _fixture.Client.Indices.RefreshAsync("marketplace-products");

        // Assert
        var response = await _fixture.Client.GetAsync<ProductSearchDocument>(
            productId.ToString(), g => g.Index("marketplace-products"));

        response.Found.Should().BeTrue();
        var retrieved = response.Source!;
        retrieved.Name.Should().Be("Updated Name");
        retrieved.Currency.Should().Be("USD"); // Currency is not changed by metadata update
    }
}
