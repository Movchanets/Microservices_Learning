using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Media;
using MassTransit;
using Media.API.Application;
using Media.API.Domain;
using Media.API.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Media.API.Application.Commands.BulkLinkMedia;

public sealed class BulkLinkMediaHandler(
    IMediaRepository mediaRepository,
    IGalleryRepository galleryRepository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork,
    ILogger<BulkLinkMediaHandler> logger)
    : IRequestHandler<BulkLinkMediaCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        BulkLinkMediaCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify media item exists
        var mediaItem = await mediaRepository.GetByIdAsync(request.MediaItemId, cancellationToken);
        if (mediaItem is null)
            return Result<bool>.Failure("Media item not found.", "NOT_FOUND");

        var newEntries = new List<GalleryEntry>();
        var events = new List<MediaUploadedIntegrationEvent>();

        // 2. Process each SKU
        foreach (var skuId in request.SkuIds.Distinct())
        {
            var existingEntries = await galleryRepository.GetByTargetAsync(
                skuId, "SKU", cancellationToken);

            // Prevent linking the same media item twice to the same SKU
            if (existingEntries.Any(e => e.MediaItemId == request.MediaItemId))
                continue;

            // Handle primary unset
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
                skuId,
                "SKU",
                existingEntries.Count, // Append to end
                request.IsPrimary,
                skuId); // Pass SkuId specifically

            newEntries.Add(galleryEntry);

            events.Add(new MediaUploadedIntegrationEvent(
                mediaItem.Id,
                skuId,
                "SKU",
                mediaItem.GetMediaUrl(),
                mediaItem.GetThumbnailUrl(),
                request.IsPrimary,
                DateTime.UtcNow));
        }

        if (newEntries.Count == 0)
            return Result<bool>.Success(true); // Nothing to do

        // 3. Persist and Publish
        foreach (var entry in newEntries)
        {
            galleryRepository.Add(entry);
        }

        foreach (var evt in events)
        {
            await publishEndpoint.Publish(evt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bulk linked media {MediaId} to {Count} SKUs",
            request.MediaItemId, newEntries.Count);

        return Result<bool>.Success(true);
    }
}
