using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Catalog.UnitTests.Infrastructure;

/// <summary>
/// Tests for ProductReadRepository projections — specifically the read-time fallback
/// that uses the first primary SKU image when Product.ImageUrl is null.
/// </summary>
public sealed class ProductReadRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CatalogDbContext _context;
    private readonly IProductReadRepository _repository;
    private readonly Guid _categoryId;

    public ProductReadRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CatalogDbContext(options);
        _context.Database.EnsureCreated();

        var category = Category.Create("Test Category");
        _context.Categories.Add(category);
        _context.SaveChanges();
        _categoryId = category.Id;

        _repository = new ProductReadRepository(_context);
    }

    [Fact]
    public async Task ListAsync_ProductWithNullImageUrl_FallsBackToFirstSkuImage()
    {
        // Arrange — product has no ImageUrl, but its SKU does
        var product = Product.Create("Fallback Product", "Description", _categoryId, Guid.NewGuid());
        var sku = product.AddSku("SKU-FB-001", Money.Create(99m, "USD"),
            new Dictionary<string, string> { { "Color", "Blue" } });
        sku.SetImageUrl("/api/media/sku-image.jpg");
        product.Activate();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ListAsync(1, 10, status: "All", ct: CancellationToken.None);

        // Assert
        var dto = result.Items.Should().ContainSingle()
            .Which;
        dto.ImageUrl.Should().Be("/api/media/sku-image.jpg");
    }

    [Fact]
    public async Task ListAsync_ProductWithOwnImageUrl_ReturnsOwnUrl()
    {
        // Arrange — product has its own ImageUrl, should not use SKU fallback
        var product = Product.Create("Own Image Product", "Description", _categoryId, Guid.NewGuid(),
            imageUrl: "/api/media/product-image.jpg");
        var sku = product.AddSku("SKU-OI-001", Money.Create(50m, "USD"),
            new Dictionary<string, string> { { "Color", "Red" } });
        sku.SetImageUrl("/api/media/sku-image.jpg");
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ListAsync(1, 10, status: "All", ct: CancellationToken.None);

        // Assert
        var dto = result.Items.Should().ContainSingle()
            .Which;
        dto.ImageUrl.Should().Be("/api/media/product-image.jpg");
    }

    [Fact]
    public async Task GetByIdAsync_ProductWithNullImageUrl_FallsBackToFirstSkuImage()
    {
        // Arrange
        var product = Product.Create("Detail Fallback", "Description", _categoryId, Guid.NewGuid());
        var sku = product.AddSku("SKU-DT-001", Money.Create(199m, "USD"),
            new Dictionary<string, string> { { "Storage", "256GB" } });
        sku.SetImageUrl("/api/media/detail-sku.jpg");
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var dto = await _repository.GetByIdAsync(product.Id, CancellationToken.None);

        // Assert
        dto.Should().NotBeNull();
        dto!.ImageUrl.Should().Be("/api/media/detail-sku.jpg");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
