using BuildingBlocks.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Aggregates;

namespace Ordering.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Ordering bounded context.
/// Manages Order aggregate roots and OrderState (saga state) entities.
/// Inherits from DomainEventsDbContext for automatic domain event dispatch.
/// The OrderState table is used by MassTransit's saga state machine.
/// </summary>
public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DomainEventsDbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderState> OrderStates => Set<OrderState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);

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

        // Saga state entity
        modelBuilder.Entity<OrderState>(b =>
        {
            b.HasKey(x => x.CorrelationId);
            b.Property(x => x.CurrentState).IsRequired().HasMaxLength(64);
            b.Property(x => x.BuyerId).IsRequired();
            b.Property(x => x.ItemsJson).HasMaxLength(4000);
            b.Property(x => x.RowVersion).IsRowVersion()
                .HasDefaultValueSql("gen_random_uuid()::text::bytea");
        });
    }
}
