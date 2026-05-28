using BuildingBlocks.Infrastructure.Models;
using Media.API.Application;
using Media.API.Application.DTOs;
using Media.API.Domain;
using MediatR;

namespace Media.API.Application.Queries.GetGallery;

public sealed class GetGalleryHandler(
    IGalleryRepository galleryRepository,
    IMediaRepository mediaRepository)
    : IRequestHandler<GetGalleryQuery, Result<List<MediaItemDto>>>
{
    public async Task<Result<List<MediaItemDto>>> Handle(
        GetGalleryQuery request, CancellationToken cancellationToken)
    {
        var entries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        if (entries.Count == 0)
            return Result<List<MediaItemDto>>.Success([]);

        var mediaIds = entries.Select(e => e.MediaItemId).ToList();
        var mediaItems = await mediaRepository.GetByIdsAsync(mediaIds, cancellationToken);
        var mediaLookup = mediaItems.ToDictionary(x => x.Id);

        var dtos = entries
            .Where(e => mediaLookup.ContainsKey(e.MediaItemId))
            .Select(e =>
            {
                var media = mediaLookup[e.MediaItemId];
                return new MediaItemDto(
                    media.Id,
                    media.FileName,
                    media.ContentType,
                    media.GetMediaUrl(),
                    media.GetThumbnailUrl(),
                    media.SizeBytes,
                    media.Type.ToString(),
                    e.SortOrder,
                    e.IsPrimary,
                    media.CreatedAt);
            })
            .ToList();

        return Result<List<MediaItemDto>>.Success(dtos);
    }
}
