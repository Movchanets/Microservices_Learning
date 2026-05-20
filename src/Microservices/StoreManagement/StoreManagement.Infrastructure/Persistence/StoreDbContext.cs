using BuildingBlocks.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Infrastructure.Persistence;

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
    }
}
