using BuildingBlocks.SharedContracts.Events.Media;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Messaging.Consumers;
using Catalog.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Catalog.UnitTests.Infrastructure;

public sealed class GalleryUpdatedConsumerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CatalogDbContext _context;
    private readonly GalleryUpdatedConsumer _consumer;
    private readonly Guid _categoryId;

    public GalleryUpdatedConsumerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CatalogDbContext(options);
        _context.Database.EnsureCreated();

        // Seed a category so Product FK is satisfied
        var category = Category.Create("Test Category");
        _context.Categories.Add(category);
        _context.SaveChanges();
        _categoryId = category.Id;

        var logger = Mock.Of<ILogger<GalleryUpdatedConsumer>>();
        _consumer = new GalleryUpdatedConsumer(_context, logger);
    }

    [Fact]
    public async Task Consume_PrimaryImageForProduct_UpdatesProductImageUrl()
    {
        // Arrange
        var product = Product.Create("Test Product", "Description", _categoryId, Guid.NewGuid());
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: product.Id,
            TargetType: "Product",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/primary.jpg", null, 0, true),
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/secondary.jpg", null, 1, false)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        var updated = await _context.Products.FindAsync(product.Id);
        updated!.ImageUrl.Should().Be("https://cdn.example.com/primary.jpg");
    }

    [Fact]
    public async Task Consume_PrimaryImageForSku_UpdatesSkuImageUrl()
    {
        // Arrange
        var product = Product.Create("Test Product", "Description", _categoryId, Guid.NewGuid());
        var sku = product.AddSku("SKU-001", Money.Create(10m, "USD"), new Dictionary<string, string> { { "Color", "Red" } });
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: sku.Id,
            TargetType: "SKU",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/sku-primary.jpg", null, 0, true)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert — SKU updated AND propagated to parent product
        var updatedSku = await _context.Skus.FindAsync(sku.Id);
        updatedSku!.ImageUrl.Should().Be("https://cdn.example.com/sku-primary.jpg");

        var updatedProduct = await _context.Products.FindAsync(product.Id);
        updatedProduct!.ImageUrl.Should().Be("https://cdn.example.com/sku-primary.jpg");
    }

    [Fact]
    public async Task Consume_NoPrimaryItem_DoesNothing()
    {
        // Arrange
        var product = Product.Create("Test Product", "Description", _categoryId, Guid.NewGuid());
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: product.Id,
            TargetType: "Product",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/img1.jpg", null, 0, false),
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/img2.jpg", null, 1, false)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        var unchanged = await _context.Products.FindAsync(product.Id);
        unchanged!.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Consume_PrimarySkuImage_ProductHasNoImageUrl_PropagatesToProduct()
    {
        // Arrange — product.ImageUrl is null (seeder scenario)
        var product = Product.Create("Test Product", "Description", _categoryId, Guid.NewGuid());
        var sku = product.AddSku("SKU-001", Money.Create(10m, "USD"), new Dictionary<string, string> { { "Color", "Red" } });
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Precondition
        product.ImageUrl.Should().BeNull();

        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: sku.Id,
            TargetType: "SKU",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "/api/media/sku-primary.jpg", null, 0, true)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert — both SKU and parent Product updated
        var updatedSku = await _context.Skus.FindAsync(sku.Id);
        updatedSku!.ImageUrl.Should().Be("/api/media/sku-primary.jpg");

        var updatedProduct = await _context.Products.FindAsync(product.Id);
        updatedProduct!.ImageUrl.Should().Be("/api/media/sku-primary.jpg");
    }

    [Fact]
    public async Task Consume_PrimarySkuImage_ProductAlreadyHasImageUrl_DoesNotOverwrite()
    {
        // Arrange — product already has a valid /api/media/ ImageUrl
        var product = Product.Create("Test Product", "Description", _categoryId, Guid.NewGuid(),
            imageUrl: "/api/media/existing.jpg");
        var sku = product.AddSku("SKU-001", Money.Create(10m, "USD"), new Dictionary<string, string> { { "Color", "Red" } });
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: sku.Id,
            TargetType: "SKU",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "/api/media/new-sku-image.jpg", null, 0, true)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert — SKU updated, product keeps its existing image
        var updatedSku = await _context.Skus.FindAsync(sku.Id);
        updatedSku!.ImageUrl.Should().Be("/api/media/new-sku-image.jpg");

        var updatedProduct = await _context.Products.FindAsync(product.Id);
        updatedProduct!.ImageUrl.Should().Be("/api/media/existing.jpg");
    }

    [Fact]
    public async Task Consume_ProductNotFound_DoesNotThrow()
    {
        // Arrange
        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: Guid.NewGuid(),
            TargetType: "Product",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/img.jpg", null, 0, true)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert — should not throw
        await _consumer.Consume(consumeContext.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
