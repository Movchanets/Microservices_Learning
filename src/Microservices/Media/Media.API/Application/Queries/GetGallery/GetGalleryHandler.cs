using BuildingBlocks.Infrastructure.Models;
using Media.API.Application;
using Media.API.Application.DTOs;
using Media.API.Domain;
using MediatR;

namespace Media.API.Application.Queries.GetGallery;

/// <summary>
/// Returns all gallery items for a target (Product/SKU), ordered by SortOrder.
///
/// This is a read-only query — no domain events, no side effects.
/// Used by:
///   - BFF (ProductBffService) to enrich product detail pages
///   - Media endpoints to display gallery in admin UI
///
/// Returns empty list if no gallery entries exist (not an error).
/// </summary>
public sealed class GetGalleryHandler(
    IGalleryRepository galleryRepository,
    IMediaRepository mediaRepository)
    : IRequestHandler<GetGalleryQuery, Result<List<MediaItemDto>>>
{
    public async Task<Result<List<MediaItemDto>>> Handle(
        GetGalleryQuery request, CancellationToken cancellationToken)
    {
        // ── Load gallery entries (ordered by SortOrder) ──────────
        var entries = await galleryRepository.GetByTargetAsync(
            request.TargetId, request.TargetType, cancellationToken);

        if (entries.Count == 0)
            return Result<List<MediaItemDto>>.Success([]);

        // ── Load associated media items ──────────────────────────
        var mediaIds = entries.Select(e => e.MediaItemId).ToList();
        var mediaItems = await mediaRepository.GetByIdsAsync(mediaIds, cancellationToken);
        var mediaLookup = mediaItems.ToDictionary(x => x.Id);

        // ── Join entries with media, build DTOs ──────────────────
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
