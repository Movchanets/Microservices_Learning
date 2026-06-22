using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Repositories;
using Catalog.IntegrationTests.Fixtures;
using FluentAssertions;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.IntegrationTests;

[Collection("Database collection")]
public class OutboxIntegrationTests
{
    private readonly CatalogDatabaseFixture _fixture;

    public OutboxIntegrationTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChangesAsync_WithDomainEvents_ShouldWriteToMassTransitOutbox()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Catalog.Infrastructure.Persistence.CatalogDbContext>(scope.ServiceProvider);
        var repository = new ProductRepository(context);

        var category = Category.Create("Outbox Category", "Outbox Test");
        context.Categories.Add(category);
        await context.SaveChangesAsync(); // No domain events on Category, so outbox should be empty from this

        var storeId = Guid.NewGuid();

        // Product creation adds a ProductCreatedDomainEvent to the entity
        var product = Product.Create(
            "Outbox Product",
            "Outbox Description",
            category.Id,
            storeId
        );

        product.AddSku(
            $"SKU-OUTBOX-{Guid.NewGuid().ToString().Substring(0, 5)}",
            Money.Create(15.99m, "USD"),
            new Dictionary<string, string>()
        );

        repository.Add(product);

        // Act
        // This should trigger the save, which dispatches the event via MediatR
        // which publishes via MT, which should write to OutboxMessage
        await context.SaveChangesAsync();

        // Assert
        // We use EF Core Set to access the OutboxMessage directly
        var outboxMessages = await context.Set<OutboxMessage>().ToListAsync();

        outboxMessages.Should().NotBeEmpty();
        outboxMessages.Count.Should().BeGreaterThan(0);

        // Since we created a product, there must be at least one message about it in the Outbox
        // In a real environment, MassTransit might have processed it and removed it if Outbox is configured to remove delivered,
        // but in this test fixture, we don't have the MassTransit background service running to consume the outbox,
        // so the message should remain in the database.
    }

    [Fact]
    public async Task SaveChangesAsync_WhenSkuAdded_ShouldPublishSkuCreatedIntegrationEvent()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Catalog.Infrastructure.Persistence.CatalogDbContext>(scope.ServiceProvider);
        var repository = new ProductRepository(context);

        var category = Category.Create("Media Category");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var storeId = Guid.NewGuid();
        var product = Product.Create("Media Product", "Media Desc", category.Id, storeId);
        
        // Act: Add an SKU
        var skuCode = "MEDIA-SKU-001";
        product.AddSku(
            skuCode,
            Money.Create(99.99m, "USD"),
            new Dictionary<string, string> { { "color", "red" } }
        );

        repository.Add(product);
        await context.SaveChangesAsync();

        // Assert: Verify SkuCreatedIntegrationEvent is in the Outbox
        var outboxMessages = await context.Set<OutboxMessage>().ToListAsync();
        
        outboxMessages.Should().NotBeEmpty();
        
        // Outbox body contains serialized integration event, so we can verify if it contains the SKU code
        // and event class name SkuCreatedIntegrationEvent
        bool containsSkuCreatedEvent = outboxMessages.Any(m => 
            m.Body.Contains("SkuCreatedIntegrationEvent") && 
            m.Body.Contains(skuCode));

        containsSkuCreatedEvent.Should().BeTrue("Because creating an SKU should publish a SkuCreatedIntegrationEvent to MassTransit outbox so Media service can create galleries for it.");
    }
}
