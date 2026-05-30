using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Events.Media;
using BuildingBlocks.SharedContracts.Abstractions;
using MassTransit;
using Media.API.Application;
using Media.API.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Media.API.Application.Commands.SetPrimaryMedia;

/// <summary>
/// Sets a specific media item as the primary (thumbnail) image for a target.
///
/// Flow:
///   1. Load gallery entries for the target
///   2. Verify the target media item exists in the gallery
///   3. Unset all entries as primary, then set the target one
///   4. Build GalleryUpdatedIntegrationEvent with full gallery state
///   5. Publish event (Outbox captures atomically with SaveChanges)
///
/// This triggers GalleryUpdatedConsumer in Catalog (updates Product/SKU.ImageUrl)
/// and MediaGalleryUpdatedConsumer in Search (updates Elasticsearch ImageUrl).
/// </summary>
public sealed class SetPrimaryMediaHandler(
    IGalleryRepository galleryRepository,
    IMediaRepository mediaRepository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork,
    ILogger<SetPrimaryMediaHandler> logger)
    : IRequestHandler<SetPrimaryMediaCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        SetPrimaryMediaCommand request, CancellationToken cancellationToken)
    {
        // ── Step 1: Load gallery entries ─────────────────────────
        var entries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        if (entries.Count == 0)
            return Result<bool>.Failure(
                "No gallery entries found for this target.", "NOT_FOUND");

        // ── Step 2: Verify target media exists in gallery ────────
        var targetEntry = entries.FirstOrDefault(e => e.MediaItemId == request.MediaItemId);
        if (targetEntry is null)
            return Result<bool>.Failure(
                "Media item not found in this gallery.", "NOT_FOUND");

        // ── Step 3: Unset all, set the target one ────────────────
        foreach (var entry in entries)
            entry.SetPrimary(entry.MediaItemId == request.MediaItemId);

        galleryRepository.UpdateRange(entries);

        // ── Step 4: Build integration event with full gallery state ──
        var galleryContracts = await BuildGalleryContractsAsync(entries, cancellationToken);

        // ── Step 5: Publish + Commit (Outbox pattern) ────────────
        await publishEndpoint.Publish(new GalleryUpdatedIntegrationEvent(
            request.TargetId,
            request.TargetType,
            galleryContracts,
            DateTime.UtcNow), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Set primary media {MediaId} for {TargetType}/{TargetId}",
            request.MediaItemId, request.TargetType, request.TargetId);

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
