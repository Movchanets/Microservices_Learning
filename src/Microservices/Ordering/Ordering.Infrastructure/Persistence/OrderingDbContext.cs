using BuildingBlocks.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Aggregates;

namespace Ordering.Infrastructure.Persistence;

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
