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
    private static readonly Dictionary<string, MediaType> ContentTypeToMediaType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = MediaType.Image,
        ["image/png"] = MediaType.Image,
        ["image/gif"] = MediaType.Image,
        ["image/webp"] = MediaType.Image,
        ["video/mp4"] = MediaType.Video
    };

    public async Task<Result<MediaItemDto>> Handle(
        UploadMediaCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "UploadMedia received: FileName={FileName}, TargetType={TargetType}, TargetId={TargetId}, IsPrimary={IsPrimary}, ContentType={ContentType}",
            request.FileName, request.TargetType, request.TargetId, request.IsPrimary, request.ContentType);

        if (!ContentTypeToMediaType.TryGetValue(request.ContentType, out var mediaType))
            return Result<MediaItemDto>.Failure(
                $"Content type '{request.ContentType}' is not allowed.", "INVALID_CONTENT_TYPE");

        var maxSize = mediaType == MediaType.Video ? 100L * 1024 * 1024 : 10L * 1024 * 1024;
        if (request.FileStream.Length > maxSize)
            return Result<MediaItemDto>.Failure(
                $"File size exceeds maximum of {maxSize / 1024 / 1024}MB.", "FILE_TOO_LARGE");

        // Upload original file
        var uploadResult = await storageService.UploadAsync(
            request.FileStream, request.FileName, request.ContentType, cancellationToken);

        // Generate thumbnail for images
        string? thumbnailBlobName = null;
        if (mediaType == MediaType.Image)
        {
            try
            {
                request.FileStream.Position = 0;
                var thumbStream = await imageProcessingService.CreateThumbnailAsync(
                    request.FileStream, cancellationToken);
                var thumbResult = await storageService.UploadAsync(
                    thumbStream, $"thumb_{request.FileName}", "image/jpeg", cancellationToken);
                thumbnailBlobName = thumbResult.BlobName;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to generate thumbnail for {FileName}, continuing without", request.FileName);
            }
        }

        // Create domain entity with API-served URL (not blob URL)
        var mediaItem = MediaItem.Create(
            request.FileName,
            request.ContentType,
            uploadResult.BlobName,
            uploadResult.Url, // placeholder, overridden below
            uploadResult.SizeBytes,
            mediaType,
            thumbnailBlobName,
            request.CreatedBy);

        mediaItem.SetUrl(mediaItem.GetMediaUrl());

        // Single query — reuse for both primary check and sort order (#9 fix)
        var existingEntries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        // If setting as primary, unset existing primary
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
            existingEntries.Count,
            request.IsPrimary);

        mediaRepository.Add(mediaItem);
        galleryRepository.Add(galleryEntry);

        logger.LogInformation(
            "GalleryEntry created: Id={EntryId}, MediaItemId={MediaItemId}, TargetType={TargetType}, TargetId={TargetId}, SortOrder={SortOrder}, IsPrimary={IsPrimary}",
            galleryEntry.Id, galleryEntry.MediaItemId, galleryEntry.TargetType, galleryEntry.TargetId, galleryEntry.SortOrder, galleryEntry.IsPrimary);

        // Publish BEFORE SaveChanges — MassTransit outbox captures this atomically
        await publishEndpoint.Publish(new MediaUploadedIntegrationEvent(
            mediaItem.Id,
            request.TargetId,
            request.TargetType,
            mediaItem.GetMediaUrl(),
            mediaItem.GetThumbnailUrl(),
            request.IsPrimary,
            DateTime.UtcNow), cancellationToken);

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
}
