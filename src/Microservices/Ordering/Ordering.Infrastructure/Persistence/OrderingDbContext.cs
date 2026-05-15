using BuildingBlocks.SharedContracts.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Aggregates;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);

        // MassTransit Outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Saga state table is configured via AddSagaStateMachine().EntityFrameworkRepository()
        // in the API's MassTransit registration. The table is created by EF migrations.
    }
}
