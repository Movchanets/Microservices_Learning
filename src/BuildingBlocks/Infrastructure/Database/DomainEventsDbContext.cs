using BuildingBlocks.SharedContracts.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Database;

/// <summary>
/// Base DbContext that implements IUnitOfWork with explicit transaction management.
/// Domain events are dispatched by <see cref="Interceptors.DomainEventDispatcherInterceptor"/>
/// which runs BEFORE SaveChanges, allowing MassTransit Outbox to write into the same transaction.
/// The OutboxState.RowVersion concurrency token is disabled in each service's DbContext
/// to prevent DbUpdateConcurrencyException.
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
}
