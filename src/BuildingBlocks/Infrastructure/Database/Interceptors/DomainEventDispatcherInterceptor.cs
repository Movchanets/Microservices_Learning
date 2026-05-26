using BuildingBlocks.SharedContracts.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BuildingBlocks.Infrastructure.Database.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that dispatches domain events via MediatR
/// BEFORE the database save occurs. This allows MassTransit Outbox to write
/// its messages into the same transaction atomically.
///
/// Note: The OutboxState.RowVersion concurrency token is disabled in each
/// service's DbContext to prevent DbUpdateConcurrencyException when multiple
/// domain events fire in a single SaveChanges call. See the DbContext
/// OnModelCreating override where AddOutboxStateEntity() is called.
///
/// Handles cascading events (events that trigger new events) via a while loop.
/// Register as a Singleton — uses DbContext.GetService to resolve IPublisher
/// from the exact DI scope that leased the DbContext from the pool.
/// </summary>
public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        throw new NotSupportedException(
            "Synchronous database saves are not supported. Use SaveChangesAsync.");
    }

    private static async Task DispatchDomainEventsAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var publisher = dbContext.GetService<IPublisher>();

        if (publisher is null) return;

        while (true)
        {
            var domainEntities = dbContext.ChangeTracker
                .Entries<AggregateRoot>()
                .Where(x => x.Entity.DomainEvents is { Count: > 0 })
                .ToList();

            if (domainEntities.Count == 0)
                break;

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            foreach (var entity in domainEntities)
            {
                entity.Entity.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
