using BuildingBlocks.Infrastructure.Database;
using Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Inventory bounded context.
/// Manages InventoryItem entities that track stock quantities per SKU per warehouse.
/// Inherits from DomainEventsDbContext for automatic domain event dispatch after persistence.
/// </summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DomainEventsDbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        // Add MassTransit Outbox configuration
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