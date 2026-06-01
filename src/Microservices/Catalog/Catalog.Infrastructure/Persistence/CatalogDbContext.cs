using BuildingBlocks.Infrastructure.Database;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(
    DbContextOptions<CatalogDbContext> options)
    : DomainEventsDbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // MassTransit Outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        // Disable RowVersion concurrency token on OutboxState to prevent
        // DbUpdateConcurrencyException when domain event handlers call
        // IPublishEndpoint.Publish() inside SaveChanges (OutboxState.RowVersion
        // gets mutated multiple times in the same transaction).
        modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.OutboxState")
            .Property<byte[]>("RowVersion")
            .IsConcurrencyToken(false);
    }
}
