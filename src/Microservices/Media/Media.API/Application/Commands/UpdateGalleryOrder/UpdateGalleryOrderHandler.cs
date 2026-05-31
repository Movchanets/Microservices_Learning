using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Events.Media;
using BuildingBlocks.SharedContracts.Abstractions;
using MassTransit;
using Media.API.Application;
using Media.API.Application.DTOs;
using Media.API.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Media.API.Application.Commands.UpdateGalleryOrder;

/// <summary>
/// Reorders gallery entries for a target (Product/SKU).
///
/// Flow:
///   1. Load existing gallery entries for the target
///   2. Apply new sort orders from the request
///   3. Build GalleryUpdatedIntegrationEvent with full gallery state
///   4. Publish event (Outbox captures atomically with SaveChanges)
///
/// The integration event carries the FULL gallery state (not just the delta)
/// so consumers can rebuild the entire gallery without additional queries.
/// </summary>
public sealed class UpdateGalleryOrderHandler(
    IGalleryRepository galleryRepository,
    IMediaRepository mediaRepository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork,
    ILogger<UpdateGalleryOrderHandler> logger)
    : IRequestHandler<UpdateGalleryOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        UpdateGalleryOrderCommand request, CancellationToken cancellationToken)
    {
        // ── Step 1: Load existing entries ────────────────────────
        var entries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        if (entries.Count == 0)
            return Result<bool>.Failure(
                "No gallery entries found for this target.", "NOT_FOUND");

        // ── Step 2: Apply new sort orders ────────────────────────
        var orderLookup = request.Items.ToDictionary(x => x.MediaItemId, x => x.SortOrder);

        foreach (var entry in entries)
        {
            if (orderLookup.TryGetValue(entry.MediaItemId, out var newOrder))
                entry.SetSortOrder(newOrder);
        }

        galleryRepository.UpdateRange(entries);

        // ── Step 3: Build integration event with full gallery state ──
        var galleryContracts = await BuildGalleryContractsAsync(entries, cancellationToken);

        // ── Step 4: Publish + Commit (Outbox pattern) ────────────
        await publishEndpoint.Publish(new GalleryUpdatedIntegrationEvent(
            request.TargetId,
            request.TargetType,
            galleryContracts,
            DateTime.UtcNow), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated gallery order for {TargetType}/{TargetId} ({Count} items)",
            request.TargetType, request.TargetId, entries.Count);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Builds GalleryItemContract list by joining gallery entries with media items.
    /// Uses the API-served URLs (not blob URLs) via MediaUrlExtensions.
    /// </summary>
    private async Task<List<GalleryItemContract>> BuildGalleryContractsAsync(
        List<Domain.Entities.GalleryEntry> entries, CancellationToken ct)
    {
        var mediaIds = entries.Select(e => e.MediaItemId).ToList();
        var mediaItems = await mediaRepository.GetByIdsAsync(mediaIds, ct);
        var mediaLookup = mediaItems.ToDictionary(x => x.Id);

        return entries
            .Where(e => mediaLookup.ContainsKey(e.MediaItemId))
            .Select(e =>
            {
                var media = mediaLookup[e.MediaItemId];
                return new GalleryItemContract(
                    media.Id,
                    media.GetMediaUrl(),
                    media.GetThumbnailUrl(),
                    e.SortOrder,
                    e.IsPrimary);
            })
            .ToList();
    }
}
