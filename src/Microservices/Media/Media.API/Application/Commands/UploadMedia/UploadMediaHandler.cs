using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Media;
using MassTransit;
using Media.API.Application;
using Media.API.Application.DTOs;
using Media.API.Application.Interfaces;
using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Domain.Enums;
using Media.API.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Media.API.Application.Commands.UploadMedia;

/// <summary>
/// Handles media upload: validates file, stores in blob storage, generates thumbnail,
/// creates domain entities, publishes integration event, and commits via Outbox.
///
/// Flow:
///   1. Validate content type and file size
///   2. Upload original file to blob storage
///   3. Generate thumbnail (images only, non-fatal if fails)
///   4. Create MediaItem domain entity
///   5. Handle primary image logic (unset existing primary if needed)
///   6. Create GalleryEntry linking media to target
///   7. Publish MediaUploadedIntegrationEvent (captured by Outbox)
///   8. SaveChanges (Outbox atomically captures event + entities)
/// </summary>
public sealed class UploadMediaHandler(
    IMediaRepository mediaRepository,
    IGalleryRepository galleryRepository,
    IMediaStorageService storageService,
    ImageProcessingService imageProcessingService,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork,
    ILogger<UploadMediaHandler> logger)
    : IRequestHandler<UploadMediaCommand, Result<MediaItemDto>>
{
    // ── Allowed Content Types ────────────────────────────────────

    private static readonly Dictionary<string, MediaType> ContentTypeToMediaType = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = MediaType.Image,
        ["image/png"] = MediaType.Image,
        ["image/gif"] = MediaType.Image,
        ["image/webp"] = MediaType.Image,
        ["video/mp4"] = MediaType.Video
    };

    // ── Handler ──────────────────────────────────────────────────

    public async Task<Result<MediaItemDto>> Handle(
        UploadMediaCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "UploadMedia received: FileName={FileName}, TargetType={TargetType}, " +
            "TargetId={TargetId}, IsPrimary={IsPrimary}, ContentType={ContentType}",
            request.FileName, request.TargetType, request.TargetId,
            request.IsPrimary, request.ContentType);

        // ── Step 1: Validate content type ────────────────────────
        if (!ContentTypeToMediaType.TryGetValue(request.ContentType, out var mediaType))
            return Result<MediaItemDto>.Failure(
                $"Content type '{request.ContentType}' is not allowed.",
                "INVALID_CONTENT_TYPE");

        // ── Step 2: Validate file size ───────────────────────────
        var maxSize = mediaType == MediaType.Video
            ? 100L * 1024 * 1024   // 100MB for video
            : 10L * 1024 * 1024;   // 10MB for images

        if (request.FileStream.Length > maxSize)
            return Result<MediaItemDto>.Failure(
                $"File size exceeds maximum of {maxSize / 1024 / 1024}MB.",
                "FILE_TOO_LARGE");

        // ── Step 3: Upload original to blob storage ──────────────
        var uploadResult = await storageService.UploadAsync(
            request.FileStream, request.FileName, request.ContentType, cancellationToken);

        // ── Step 4: Generate thumbnail (images only) ─────────────
        string? thumbnailBlobName = null;
        if (mediaType == MediaType.Image)
        {
            thumbnailBlobName = await TryGenerateThumbnailAsync(
                request.FileStream, request.FileName, cancellationToken);
        }

        // ── Step 5: Create domain entity ─────────────────────────
        var mediaItem = MediaItem.Create(
            request.FileName,
            request.ContentType,
            uploadResult.BlobName,
            uploadResult.Url,      // placeholder — overridden below
            uploadResult.SizeBytes,
            mediaType,
            thumbnailBlobName,
            request.CreatedBy);

        // Set the API-served URL (not the raw blob URL)
        mediaItem.SetUrl(mediaItem.GetMediaUrl());

        // ── Step 6: Handle primary image + gallery entry ─────────
        var existingEntries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        // If setting as primary, unset existing primary entries
        if (request.IsPrimary)
        {
            var primaries = existingEntries.Where(e => e.IsPrimary).ToList();
            foreach (var entry in primaries)
                entry.SetPrimary(false);
            if (primaries.Count > 0)
                galleryRepository.UpdateRange(primaries);
        }

        var galleryEntry = GalleryEntry.Create(
            mediaItem.Id,
            request.TargetId,
            request.TargetType,
            existingEntries.Count,  // sort order = append to end
            request.IsPrimary,
            request.TargetType.Equals("SKU", StringComparison.OrdinalIgnoreCase) ? request.TargetId : null);

        mediaRepository.Add(mediaItem);
        galleryRepository.Add(galleryEntry);

        logger.LogInformation(
            "GalleryEntry created: Id={EntryId}, MediaItemId={MediaItemId}, " +
            "TargetType={TargetType}, TargetId={TargetId}, SortOrder={SortOrder}, " +
            "IsPrimary={IsPrimary}",
            galleryEntry.Id, galleryEntry.MediaItemId, galleryEntry.TargetType,
            galleryEntry.TargetId, galleryEntry.SortOrder, galleryEntry.IsPrimary);

        // ── Step 7: Publish integration event (Outbox captures atomically) ──
        await publishEndpoint.Publish(new MediaUploadedIntegrationEvent(
            mediaItem.Id,
            request.TargetId,
            request.TargetType,
            mediaItem.GetMediaUrl(),
            mediaItem.GetThumbnailUrl(),
            request.IsPrimary,
            DateTime.UtcNow), cancellationToken);

        // ── Step 8: Commit (Outbox atomically saves event + entities) ───────
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Uploaded media {MediaId} for {TargetType}/{TargetId}",
            mediaItem.Id, request.TargetType, request.TargetId);

        return Result<MediaItemDto>.Success(new MediaItemDto(
            mediaItem.Id,
            mediaItem.FileName,
            mediaItem.ContentType,
            mediaItem.GetMediaUrl(),
            mediaItem.GetThumbnailUrl(),
            mediaItem.SizeBytes,
            mediaItem.Type.ToString(),
            galleryEntry.SortOrder,
            galleryEntry.IsPrimary,
            mediaItem.CreatedAt));
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Generates a thumbnail for an image. Non-fatal — if thumbnail generation
    /// fails, the upload still succeeds (just without a thumbnail).
    /// </summary>
    private async Task<string?> TryGenerateThumbnailAsync(
        Stream fileStream, string fileName, CancellationToken ct)
    {
        try
        {
            fileStream.Position = 0;
            var thumbStream = await imageProcessingService.CreateThumbnailAsync(fileStream, ct);
            var thumbResult = await storageService.UploadAsync(
                thumbStream, $"thumb_{fileName}", "image/jpeg", ct);
            return thumbResult.BlobName;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to generate thumbnail for {FileName}, continuing without", fileName);
            return null;
        }
    }
}
