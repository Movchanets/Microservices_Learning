using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Events.Media;
using BuildingBlocks.SharedContracts.Abstractions;
using MassTransit;
using Media.API.Application.Interfaces;
using Media.API.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Media.API.Application.Commands.DeleteMedia;

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
        var mediaItem = await mediaRepository.GetByIdAsync(request.MediaItemId, cancellationToken);
        if (mediaItem is null)
            return Result<bool>.Failure("Media item not found.", "NOT_FOUND");

        var galleryEntry = await galleryRepository.GetByMediaItemAsync(request.MediaItemId, cancellationToken);

        // Capture info for integration event before deletion
        var targetId = galleryEntry?.TargetId ?? Guid.Empty;
        var targetType = galleryEntry?.TargetType ?? "";
        var wasPrimary = galleryEntry?.IsPrimary ?? false;

        // Remove domain entities
        if (galleryEntry is not null)
            galleryRepository.Remove(galleryEntry);

        mediaRepository.Remove(mediaItem);

        // Publish BEFORE SaveChanges — MassTransit outbox captures this atomically
        if (galleryEntry is not null)
        {
            await publishEndpoint.Publish(new MediaDeletedIntegrationEvent(
                request.MediaItemId,
                targetId,
                targetType,
                wasPrimary,
                DateTime.UtcNow), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Delete from storage AFTER DB commit (non-transactional, best-effort)
        try
        {
            await storageService.DeleteAsync(mediaItem.BlobName, cancellationToken);
            if (mediaItem.ThumbnailBlobName is not null)
                await storageService.DeleteAsync(mediaItem.ThumbnailBlobName, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob {BlobName} from storage (non-fatal)", mediaItem.BlobName);
        }

        logger.LogInformation("Deleted media {MediaId}", request.MediaItemId);

        return Result<bool>.Success(true);
    }
}
