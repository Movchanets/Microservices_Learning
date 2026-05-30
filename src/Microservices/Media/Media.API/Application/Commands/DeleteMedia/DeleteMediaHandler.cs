using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Events.Media;
using BuildingBlocks.SharedContracts.Abstractions;
using MassTransit;
using Media.API.Application.Interfaces;
using Media.API.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Media.API.Application.Commands.DeleteMedia;

/// <summary>
/// Handles media deletion: removes domain entities, publishes integration event,
/// and deletes blob from storage.
///
/// Flow:
///   1. Look up MediaItem + GalleryEntry
///   2. Capture WasPrimary flag before deletion
///   3. Remove domain entities from DB
///   4. Publish MediaDeletedIntegrationEvent (Outbox captures atomically)
///   5. SaveChanges (commit DB + Outbox event)
///   6. Delete blob from storage (best-effort, after DB commit)
///
/// Design note: Blob deletion happens AFTER DB commit. If blob deletion fails,
/// the media is still logically deleted (not served to users). Orphaned blobs
/// can be cleaned up by a background job.
/// </summary>
public sealed class DeleteMediaHandler(
    IMediaRepository mediaRepository,
    IGalleryRepository galleryRepository,
    IMediaStorageService storageService,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork,
    ILogger<DeleteMediaHandler> logger)
    : IRequestHandler<DeleteMediaCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        // ── Step 1: Look up domain entities ──────────────────────
        var mediaItem = await mediaRepository.GetByIdAsync(
            request.MediaItemId, cancellationToken);
        if (mediaItem is null)
            return Result<bool>.Failure("Media item not found.", "NOT_FOUND");

        var galleryEntry = await galleryRepository.GetByMediaItemAsync(
            request.MediaItemId, cancellationToken);

        // ── Step 2: Capture info for integration event ───────────
        var targetId = galleryEntry?.TargetId ?? Guid.Empty;
        var targetType = galleryEntry?.TargetType ?? "";
        var wasPrimary = galleryEntry?.IsPrimary ?? false;

        // ── Step 3: Remove domain entities ───────────────────────
        if (galleryEntry is not null)
            galleryRepository.Remove(galleryEntry);

        mediaRepository.Remove(mediaItem);

        // ── Step 4: Publish integration event (Outbox) ───────────
        if (galleryEntry is not null)
        {
            await publishEndpoint.Publish(new MediaDeletedIntegrationEvent(
                request.MediaItemId,
                targetId,
                targetType,
                wasPrimary,
                DateTime.UtcNow), cancellationToken);
        }

        // ── Step 5: Commit (Outbox atomically saves event + entities) ──
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Step 6: Delete blob from storage (best-effort) ───────
        await TryDeleteBlobAsync(mediaItem.BlobName, cancellationToken);
        if (mediaItem.ThumbnailBlobName is not null)
            await TryDeleteBlobAsync(mediaItem.ThumbnailBlobName, cancellationToken);

        logger.LogInformation("Deleted media {MediaId}", request.MediaItemId);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Deletes a blob from storage. Non-fatal — if deletion fails, the media
    /// is still logically deleted (not served to users). Orphaned blobs can
    /// be cleaned up by a background job.
    /// </summary>
    private async Task TryDeleteBlobAsync(string blobName, CancellationToken ct)
    {
        try
        {
            await storageService.DeleteAsync(blobName, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to delete blob {BlobName} from storage (non-fatal)", blobName);
        }
    }
}
