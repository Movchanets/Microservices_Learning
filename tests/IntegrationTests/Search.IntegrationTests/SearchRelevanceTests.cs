using FluentAssertions;
using Search.API.Models;

namespace Search.IntegrationTests;

/// <summary>
/// Integration tests verifying search query relevance after removing 'description'
/// from searchable fields and adding 'brand' + 'attributes'.
/// Addresses: https://github.com/Movchanets/Microservices_Learning/issues/53
/// </summary>
[Collection("Search collection")]
public class SearchRelevanceTests
{
    private readonly SearchDatabaseFixture _fixture;

    public SearchRelevanceTests(SearchDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Seeds products that specifically test the description-noise problem:
    /// Sony headphones have "Apple Pay" in description but are NOT Apple products.
    /// </summary>
    private async Task SeedRelevanceProductsAsync()
    {
        var electronicsId = Guid.NewGuid();

        // Apple product — brand=Apple, name contains "Apple"
        await _fixture.SearchService.IndexProductAsync(new ProductSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = "Apple iPhone 15 Pro",
            Description = "Smartphone with A17 Pro chip",
            Brand = "Apple",
            Attributes = new Dictionary<string, string>
            {
                { "color", "Natural Titanium" },
                { "storage", "256GB" }
            },
            MinPrice = 999m, MaxPrice = 1199m, Currency = "USD", SkuCount = 2,
            CategoryId = electronicsId, CategoryName = "Electronics",
            Tags = ["smartphone", "apple"], StoreId = Guid.NewGuid(),
            IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-1), UpdatedAt = DateTime.UtcNow
        });

        // Sony headphones — NOT Apple, but description contains "Apple Pay"
        await _fixture.SearchService.IndexProductAsync(new ProductSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = "Sony WH-1000XM5 Wireless Headphones",
            Description = "Industry-leading noise cancellation. Supports Apple Pay at checkout.",
            Brand = "Sony",
            Attributes = new Dictionary<string, string>
            {
                { "color", "Black" }
            },
            MinPrice = 348m, MaxPrice = 348m, Currency = "USD", SkuCount = 1,
            CategoryId = electronicsId, CategoryName = "Electronics",
            Tags = ["headphones", "wireless", "noise-cancelling"], StoreId = Guid.NewGuid(),
            IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-2), UpdatedAt = DateTime.UtcNow
        });

        // Samsung phone — brand=Samsung, no "samsung" in name
        await _fixture.SearchService.IndexProductAsync(new ProductSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = "Galaxy S24 Ultra",
            Description = "Flagship smartphone with S Pen",
            Brand = "Samsung",
            Attributes = new Dictionary<string, string>
            {
                { "color", "Titanium Black" },
                { "storage", "512GB" }
            },
            MinPrice = 1299m, MaxPrice = 1419m, Currency = "USD", SkuCount = 2,
            CategoryId = electronicsId, CategoryName = "Electronics",
            Tags = ["smartphone", "flagship"], StoreId = Guid.NewGuid(),
            IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-3), UpdatedAt = DateTime.UtcNow
        });

        // Hator headphones — NOT Apple, no Apple reference at all
        await _fixture.SearchService.IndexProductAsync(new ProductSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = "Hator Hypergang 2 Wireless",
            Description = "Gaming headset with RGB lighting",
            Brand = "Hator",
            Attributes = new Dictionary<string, string>
            {
                { "color", "чорний" }  // Ukrainian for "black"
            },
            MinPrice = 79m, MaxPrice = 79m, Currency = "USD", SkuCount = 1,
            CategoryId = electronicsId, CategoryName = "Electronics",
            Tags = ["headphones", "gaming"], StoreId = Guid.NewGuid(),
            IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-4), UpdatedAt = DateTime.UtcNow
        });

        await _fixture.Client.Indices.RefreshAsync("marketplace-products");
    }

    [Fact]
    public async Task SearchApple_ExcludesProductsMatchingOnlyInDescription()
    {
        // Arrange
        await SeedRelevanceProductsAsync();

        // Act
        var result = await _fixture.SearchService.SearchAsync(
            new SearchRequest("apple", null, null, null, null, null, null, null, 1, 10));

        // Assert — should find Apple iPhone but NOT Sony headphones (which had "Apple Pay" in description)
        result.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p =>
            p.Name.Should().Contain("Apple", because: "only Apple-named products should match"));
        result.Items.Should().NotContain(p => p.Name.Contains("Sony"),
            because: "Sony headphones matched only via 'Apple Pay' in description");
    }

    [Fact]
    public async Task SearchSamsung_MatchesByBrandField()
    {
        // Arrange
        await SeedRelevanceProductsAsync();

        // Act — "samsung" is NOT in the product name "Galaxy S24 Ultra"
        var result = await _fixture.SearchService.SearchAsync(
            new SearchRequest("samsung", null, null, null, null, null, null, null, 1, 10));

        // Assert — should find Galaxy S24 Ultra via brand field
        result.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(p => p.Brand == "Samsung");
    }

    [Fact]
    public async Task SearchByAttributeValue_MatchesAttributeValues()
    {
        // Arrange
        await SeedRelevanceProductsAsync();

        // Act — "чорний" (Ukrainian for "black") is in the color attribute
        var result = await _fixture.SearchService.SearchAsync(
            new SearchRequest("чорний", null, null, null, null, null, null, null, 1, 10));

        // Assert — should find Hator headset via attribute value
        result.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(p => p.Brand == "Hator");
    }

    [Fact]
    public async Task SearchTypoTolerance_StillWorks()
    {
        // Arrange
        await SeedRelevanceProductsAsync();

        // Act — "aaple" is a typo for "apple"
        var result = await _fixture.SearchService.SearchAsync(
            new SearchRequest("aaple", null, null, null, null, null, null, null, 1, 10));

        // Assert — fuzzy matching should still find Apple products
        result.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(p => p.Brand == "Apple");
    }
}
