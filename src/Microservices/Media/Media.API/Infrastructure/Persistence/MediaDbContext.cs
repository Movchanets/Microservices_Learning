using BuildingBlocks.Infrastructure.Database;
using MassTransit;
using Media.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Media.API.Infrastructure.Persistence;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options)
    : DomainEventsDbContext(options)
{
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<GalleryEntry> GalleryEntries => Set<GalleryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);

        // MassTransit Outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Disable RowVersion concurrency token on OutboxState
        modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.OutboxState")
            .Property<byte[]>("RowVersion")
            .IsConcurrencyToken(false);
    }
}
