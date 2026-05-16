using FluentAssertions;
using Search.API.Models;

namespace Search.IntegrationTests;

[Collection("Search collection")]
public class SearchQueryTests
{
    private readonly SearchDatabaseFixture _fixture;

    public SearchQueryTests(SearchDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SeedProductsAsync()
    {
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();

        var products = new List<ProductSearchDocument>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Wireless Bluetooth Headphones", Description = "High quality audio",
                Price = 59.99m, Currency = "USD", Sku = "HP-001", CategoryId = categoryId1,
                CategoryName = "Electronics", Tags = ["audio", "wireless"], StoreId = Guid.NewGuid(),
                IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-1), UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Bluetooth Speaker Portable", Description = "Portable audio device",
                Price = 29.99m, Currency = "USD", Sku = "SP-001", CategoryId = categoryId1,
                CategoryName = "Electronics", Tags = ["audio", "portable"], StoreId = Guid.NewGuid(),
                IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-2), UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Running Shoes", Description = "Comfortable running shoes",
                Price = 89.99m, Currency = "USD", Sku = "SH-001", CategoryId = categoryId2,
                CategoryName = "Sports", Tags = ["running", "shoes"], StoreId = Guid.NewGuid(),
                IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-3), UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Yoga Mat Premium", Description = "Non-slip exercise mat",
                Price = 24.99m, Currency = "USD", Sku = "YM-001", CategoryId = categoryId2,
                CategoryName = "Sports", Tags = ["yoga", "exercise"], StoreId = Guid.NewGuid(),
                IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-4), UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Laptop Stand Aluminum", Description = "Ergonomic laptop stand",
                Price = 45m, Currency = "USD", Sku = "LS-001", CategoryId = categoryId1,
                CategoryName = "Electronics", Tags = ["laptop", "ergonomic"], StoreId = Guid.NewGuid(),
                IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-5), UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var product in products)
        {
            await _fixture.SearchService.IndexProductAsync(product);
        }

        // Force refresh so documents are searchable immediately
        await _fixture.Client.Indices.RefreshAsync("marketplace-products");
    }

    [Fact]
    public async Task FullTextSearch_ReturnsMatchingProducts()
    {
        // Arrange
        await SeedProductsAsync();

        // Act
        var result = await _fixture.SearchService.SearchAsync(
            "bluetooth", null, null, null, null, 1, 10);

        // Assert
        result.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(p => p.Name.Contains("Bluetooth"));
    }

    [Fact]
    public async Task FilterByCategory_ReturnsOnlyMatchingProducts()
    {
        // Arrange
        await SeedProductsAsync();

        // First get all to find a category ID
        var all = await _fixture.SearchService.SearchAsync(null, null, null, null, null, 1, 100);
        var electronicsCategoryId = all.Items.First(p => p.CategoryName == "Electronics").CategoryId;

        // Act
        var result = await _fixture.SearchService.SearchAsync(
            null, electronicsCategoryId, null, null, null, 1, 10);

        // Assert
        result.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p => p.CategoryId.Should().Be(electronicsCategoryId));
    }

    [Fact]
    public async Task Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await SeedProductsAsync();

        // Act
        var page1 = await _fixture.SearchService.SearchAsync(null, null, null, null, null, 1, 2);
        var page2 = await _fixture.SearchService.SearchAsync(null, null, null, null, null, 2, 2);

        // Assert
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.TotalCount.Should().BeGreaterThanOrEqualTo(5);

        // Ensure no overlap between pages
        var page1Ids = page1.Items.Select(p => p.Id).ToHashSet();
        var page2Ids = page2.Items.Select(p => p.Id).ToHashSet();
        page1Ids.Intersect(page2Ids).Should().BeEmpty();
    }

    [Fact]
    public async Task PriceRangeFilter_ReturnsProductsInRange()
    {
        // Arrange
        await SeedProductsAsync();

        // Act
        var result = await _fixture.SearchService.SearchAsync(
            null, null, 30m, 60m, null, 1, 100);

        // Assert
        result.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p =>
        {
            p.Price.Should().BeGreaterThanOrEqualTo(30m);
            p.Price.Should().BeLessThanOrEqualTo(60m);
        });
    }
}
