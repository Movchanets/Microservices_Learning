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
        var entries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        if (entries.Count == 0)
            return Result<bool>.Failure("No gallery entries found for this target.", "NOT_FOUND");

        var orderLookup = request.Items.ToDictionary(x => x.MediaItemId, x => x.SortOrder);

        foreach (var entry in entries)
        {
            if (orderLookup.TryGetValue(entry.MediaItemId, out var newOrder))
                entry.SetSortOrder(newOrder);
        }

        galleryRepository.UpdateRange(entries);

        // Build integration event BEFORE SaveChanges (outbox pattern)
        var mediaIds = entries.Select(e => e.MediaItemId).ToList();
        var mediaItems = await mediaRepository.GetByIdsAsync(mediaIds, cancellationToken);
        var mediaLookup = mediaItems.ToDictionary(x => x.Id);

        var galleryContracts = entries
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

        // Publish BEFORE SaveChanges — MassTransit outbox captures this atomically
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
}
