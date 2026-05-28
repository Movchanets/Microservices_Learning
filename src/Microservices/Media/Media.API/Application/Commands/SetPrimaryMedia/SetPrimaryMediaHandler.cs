using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Events.Media;
using BuildingBlocks.SharedContracts.Abstractions;
using MassTransit;
using Media.API.Application;
using Media.API.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Media.API.Application.Commands.SetPrimaryMedia;

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
        var entries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        if (entries.Count == 0)
            return Result<bool>.Failure("No gallery entries found for this target.", "NOT_FOUND");

        var targetEntry = entries.FirstOrDefault(e => e.MediaItemId == request.MediaItemId);
        if (targetEntry is null)
            return Result<bool>.Failure("Media item not found in this gallery.", "NOT_FOUND");

        // Unset all, set the target one
        foreach (var entry in entries)
            entry.SetPrimary(entry.MediaItemId == request.MediaItemId);

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
            "Set primary media {MediaId} for {TargetType}/{TargetId}",
            request.MediaItemId, request.TargetType, request.TargetId);

        return Result<bool>.Success(true);
    }
}
