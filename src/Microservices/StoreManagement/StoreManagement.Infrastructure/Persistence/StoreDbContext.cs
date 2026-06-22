using BuildingBlocks.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the StoreManagement bounded context.
/// Manages Store aggregate roots that represent seller storefronts.
/// Inherits from DomainEventsDbContext for automatic domain event dispatch.
/// </summary>
public sealed class StoreDbContext(
    DbContextOptions<StoreDbContext> options)
    : DomainEventsDbContext(options)
{
    public DbSet<Store> Stores => Set<Store>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreDbContext).Assembly);

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
