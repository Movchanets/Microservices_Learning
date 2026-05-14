using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IUnitOfWork
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
    }
}