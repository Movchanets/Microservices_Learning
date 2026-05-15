using BuildingBlocks.SharedContracts.Abstractions;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Infrastructure.Persistence;

public sealed class StoreDbContext(
    DbContextOptions<StoreDbContext> options,
    IPublisher publisher)
    : DbContext(options), IUnitOfWork
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            return await SaveChangesAndPublishDomainEventsAsync(cancellationToken);
        }

        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            var result = await SaveChangesAndPublishDomainEventsAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task<int> SaveChangesAndPublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        var domainEntities = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }

        await base.SaveChangesAsync(cancellationToken);

        return result;
    }
}
