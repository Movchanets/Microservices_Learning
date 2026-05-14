using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Models;
using Search.API.Services;
using Xunit;

namespace Search.IntegrationTests;

[CollectionDefinition("Search collection")]
public class SearchCollection : ICollectionFixture<SearchDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[Collection("Search collection")]
public class SearchIntegrationTests : IAsyncLifetime
{
    private readonly SearchDatabaseFixture _fixture;
    private readonly ElasticsearchService _searchService;
    private readonly string _indexName = "marketplace-products";

    public SearchIntegrationTests(SearchDatabaseFixture fixture)
    {
        _fixture = fixture;

        var loggerMock = new Mock<ILogger<ElasticsearchService>>();
        _searchService = new ElasticsearchService(_fixture.Client, loggerMock.Object);
    }

    public async Task InitializeAsync()
    {
        // Recreate index before each test to ensure a clean state
        if ((await _fixture.Client.Indices.ExistsAsync(_indexName)).Exists)
        {
            await _fixture.Client.Indices.DeleteAsync(_indexName);
        }

        await _fixture.Client.Indices.CreateAsync(_indexName);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IndexProductAsync_ShouldIndexProductAndRetrieveIt()
    {
        // Arrange
        var product = new ProductSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "A great product for testing.",
            Price = 19.99m,
            CategoryId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tags = ["test", "integration"]
        };

        // Act
        await _searchService.IndexProductAsync(product);

        // Refresh index to make documents available for search immediately
        await _fixture.Client.Indices.RefreshAsync(_indexName);

        var searchResult = await _searchService.SearchAsync("Test", null, null, null, null, 1, 10);

        // Assert
        searchResult.Items.Should().ContainSingle();
        searchResult.Items[0].Id.Should().Be(product.Id);
        searchResult.Items[0].Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateDocumentFields()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new ProductSearchDocument
        {
            Id = productId,
            Name = "Original Name",
            Description = "Original Description",
            Price = 10.0m,
            IsActive = true
        };

        await _searchService.IndexProductAsync(product);
        await _fixture.Client.Indices.RefreshAsync(_indexName);

        var updatedProduct = new ProductSearchDocument
        {
            Id = productId,
            Name = "Updated Name",
            Description = "Updated Description",
            Price = 20.0m,
            IsActive = true
        };

        // Act
        await _searchService.UpdateProductAsync(updatedProduct);
        await _fixture.Client.Indices.RefreshAsync(_indexName);

        var searchResult = await _searchService.SearchAsync("Updated", null, null, null, null, 1, 10);

        // Assert
        searchResult.Items.Should().ContainSingle();
        var resultProduct = searchResult.Items[0];
        resultProduct.Id.Should().Be(productId);
        resultProduct.Name.Should().Be("Updated Name");
        resultProduct.Description.Should().Be("Updated Description");
        resultProduct.Price.Should().Be(20.0m);
    }

    [Fact]
    public async Task SearchAsync_FullTextSearch_ShouldReturnCorrectDocuments()
    {
        // Arrange
        var product1 = new ProductSearchDocument { Id = Guid.NewGuid(), Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", IsActive = true };
        var product2 = new ProductSearchDocument { Id = Guid.NewGuid(), Name = "Mechanical Keyboard", Description = "Clicky mechanical keyboard", IsActive = true };
        var product3 = new ProductSearchDocument { Id = Guid.NewGuid(), Name = "Gaming Mouse", Description = "Wired gaming mouse with RGB", IsActive = true };

        await _searchService.IndexProductAsync(product1);
        await _searchService.IndexProductAsync(product2);
        await _searchService.IndexProductAsync(product3);
        await _fixture.Client.Indices.RefreshAsync(_indexName);

        // Act
        var resultMouse = await _searchService.SearchAsync("mouse", null, null, null, null, 1, 10);
        var resultKeyboard = await _searchService.SearchAsync("keyboard", null, null, null, null, 1, 10);

        // Assert
        resultMouse.Items.Should().HaveCount(2);
        resultMouse.Items.Select(i => i.Id).Should().Contain(new[] { product1.Id, product3.Id });

        resultKeyboard.Items.Should().ContainSingle();
        resultKeyboard.Items[0].Id.Should().Be(product2.Id);
    }

    [Fact]
    public async Task SearchAsync_FilteringByCategoryId_ShouldReturnExactSubset()
    {
        // Arrange
        var electronicsCategoryId = Guid.NewGuid();
        var clothingCategoryId = Guid.NewGuid();

        var p1 = new ProductSearchDocument { Id = Guid.NewGuid(), Name = "Laptop", CategoryId = electronicsCategoryId, IsActive = true };
        var p2 = new ProductSearchDocument { Id = Guid.NewGuid(), Name = "T-Shirt", CategoryId = clothingCategoryId, IsActive = true };
        var p3 = new ProductSearchDocument { Id = Guid.NewGuid(), Name = "Smartphone", CategoryId = electronicsCategoryId, IsActive = true };

        await _searchService.IndexProductAsync(p1);
        await _searchService.IndexProductAsync(p2);
        await _searchService.IndexProductAsync(p3);
        await _fixture.Client.Indices.RefreshAsync(_indexName);

        // Act
        var electronicsResult = await _searchService.SearchAsync(null, electronicsCategoryId, null, null, null, 1, 10);

        // Assert
        electronicsResult.Items.Should().HaveCount(2);
        electronicsResult.Items.Select(i => i.Id).Should().Contain(new[] { p1.Id, p3.Id });
        electronicsResult.Items.Should().NotContain(i => i.Id == p2.Id);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ShouldWorkCorrectly()
    {
        // Arrange
        var products = Enumerable.Range(1, 15).Select(i => new ProductSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = $"Paginated Product {i}",
            IsActive = true,
            // Decreasing CreatedAt so that sorting brings newest (higher i if we set it) or just use the same logic.
            // Actually, default sort is Score DESC, CreatedAt DESC.
            // By giving them different CreatedAt, we ensure stable sorting.
            CreatedAt = DateTime.UtcNow.AddMinutes(i)
        }).ToList();

        foreach (var p in products)
        {
            await _searchService.IndexProductAsync(p);
        }
        await _fixture.Client.Indices.RefreshAsync(_indexName);

        // Act
        var page1 = await _searchService.SearchAsync(null, null, null, null, null, page: 1, pageSize: 10);
        var page2 = await _searchService.SearchAsync(null, null, null, null, null, page: 2, pageSize: 10);

        // Assert
        page1.TotalCount.Should().Be(15);
        page1.Items.Should().HaveCount(10);
        page2.Items.Should().HaveCount(5);

        // Verify that the items in page1 and page2 are mutually exclusive
        var page1Ids = page1.Items.Select(i => i.Id).ToList();
        var page2Ids = page2.Items.Select(i => i.Id).ToList();
        page1Ids.Intersect(page2Ids).Should().BeEmpty();
    }
}