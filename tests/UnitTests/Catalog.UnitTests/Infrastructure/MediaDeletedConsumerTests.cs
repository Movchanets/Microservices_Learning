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

public sealed class MediaDeletedConsumerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CatalogDbContext _context;
    private readonly MediaDeletedConsumer _consumer;
    private readonly Guid _categoryId;

    public MediaDeletedConsumerTests()
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

        var logger = Mock.Of<ILogger<MediaDeletedConsumer>>();
        _consumer = new MediaDeletedConsumer(_context, logger);
    }

    [Fact]
    public async Task Consume_ProductWithImageUrl_ClearsImageUrl()
    {
        // Arrange
        var product = Product.Create("Test Product", "Description", _categoryId, Guid.NewGuid(),
            imageUrl: "https://cdn.example.com/old-image.jpg");
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var evt = new MediaDeletedIntegrationEvent(
            MediaItemId: Guid.NewGuid(),
            TargetId: product.Id,
            TargetType: "Product",
            WasPrimary: true,
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<MediaDeletedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        // Product.SetImageUrl(null) correctly clears the URL to null.
        var updated = await _context.Products.FindAsync(product.Id);
        updated!.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Consume_SkuWithImageUrl_ClearsImageUrl()
    {
        // Arrange
        var product = Product.Create("Test Product", "Description", _categoryId, Guid.NewGuid());
        var sku = product.AddSku("SKU-001", Money.Create(10m, "USD"), new Dictionary<string, string> { { "Color", "Red" } });
        sku.SetImageUrl("https://cdn.example.com/sku-image.jpg");
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var evt = new MediaDeletedIntegrationEvent(
            MediaItemId: Guid.NewGuid(),
            TargetId: sku.Id,
            TargetType: "SKU",
            WasPrimary: true,
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<MediaDeletedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        // Sku.SetImageUrl(null) correctly clears the URL to null.
        var updatedSku = await _context.Skus.FindAsync(sku.Id);
        updatedSku!.ImageUrl.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
