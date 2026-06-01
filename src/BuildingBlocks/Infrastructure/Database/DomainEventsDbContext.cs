using BuildingBlocks.SharedContracts.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Database;

/// <summary>
/// Base DbContext that implements IUnitOfWork with explicit transaction management.
/// Domain events are dispatched by <see cref="Interceptors.DomainEventDispatcherInterceptor"/>
/// which runs BEFORE SaveChanges, allowing MassTransit Outbox to write into the same transaction.
/// </summary>
public abstract class DomainEventsDbContext(DbContextOptions options)
    : DbContext(options), IUnitOfWork
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            var result = await base.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Guid v7 value generation for all Entity.Id properties.
        // This ensures new entities get time-ordered IDs on insert, and EF Core
        // correctly detects them as Added (Id is Guid.Empty until insert).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Entity.Id))
                    .HasValueGenerator<GuidV7ValueGenerator>();
            }
        }
    }
}
