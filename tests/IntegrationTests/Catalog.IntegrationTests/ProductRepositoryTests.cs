using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Repositories;
using Catalog.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.IntegrationTests;

[Collection("Database collection")]
public class ProductRepositoryTests
{
    private readonly CatalogDatabaseFixture _fixture;

    public ProductRepositoryTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Add_ShouldSaveProductAndGetByIdAsync_ShouldRetrieveIt()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Catalog.Infrastructure.Persistence.CatalogDbContext>(scope.ServiceProvider);
        var repository = new ProductRepository(context);

        var category = Category.Create("Test Category", "Test Description");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var product = Product.Create(
            "Test Product",
            "Test Description",
            99.99m,
            "USD",
            "SKU-12345",
            category.Id,
            sellerId,
            new List<string> { "tag1", "tag2" },
            "http://test.com/image.jpg"
        );

        // Act
        repository.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        var retrievedProduct = await repository.GetByIdAsync(product.Id);

        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Name.Should().Be("Test Product");
        retrievedProduct.Description.Should().Be("Test Description");
        retrievedProduct.Price.Amount.Should().Be(99.99m);
        retrievedProduct.Price.Currency.Should().Be("USD");
        retrievedProduct.Sku.Value.Should().Be("SKU-12345");
        retrievedProduct.CategoryId.Should().Be(category.Id);
        retrievedProduct.SellerId.Should().Be(sellerId);
        retrievedProduct.Tags.Should().BeEquivalentTo("tag1", "tag2");
        retrievedProduct.ImageUrl.Should().Be("http://test.com/image.jpg");
    }

    [Fact]
    public async Task PaginationAndFiltering_ShouldWorkAgainstDbContext()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Catalog.Infrastructure.Persistence.CatalogDbContext>(scope.ServiceProvider);
        var category = Category.Create("Test Category Filter", "Test Description");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var sellerId = Guid.NewGuid();

        for (int i = 0; i < 15; i++)
        {
            var product = Product.Create(
                $"Filtered Product {i}",
                "Test Description",
                10.00m + i,
                "USD",
                $"SKU-F-{i}-{Guid.NewGuid().ToString().Substring(0, 5)}", // Unique sku
                category.Id,
                sellerId
            );
            context.Products.Add(product);
        }
        await context.SaveChangesAsync();

        // Act
        var query = context.Products
            .Where(p => p.Name.Contains("Filtered Product"))
            .OrderBy(p => p.Name);

        var count = await query.CountAsync();
        var pagedItems = await query.Skip(5).Take(5).ToListAsync();

        // Assert
        count.Should().BeGreaterThanOrEqualTo(15);
        pagedItems.Should().HaveCount(5);
        pagedItems.All(p => p.Name.Contains("Filtered Product")).Should().BeTrue();
    }
}
