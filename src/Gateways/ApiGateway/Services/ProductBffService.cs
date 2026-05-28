using System.Net.Http.Headers;

namespace ApiGateway.Services;

/// <summary>
/// BFF aggregation service: fetches product/SKU from catalog-api, enriches with gallery from media-api.
/// Uses parallel fetches to minimize latency (hybrid caching pattern).
/// </summary>
public sealed class ProductBffService(
    IHttpClientFactory httpClientFactory,
    ILogger<ProductBffService> logger)
{
    /// <summary>
    /// Returns product detail enriched with gallery images from Media.API.
    /// Fetches catalog + gallery in PARALLEL to minimize PDP latency.
    /// Falls back to Product.ImageUrl if gallery is unavailable.
    /// </summary>
    public async Task<object?> GetProductWithGalleryAsync(
        Guid productId,
        CancellationToken ct = default)
    {
        return await FetchWithGalleryAsync(
            $"/api/catalog/products/{productId}",
            "Product",
            productId,
            ct);
    }

    /// <summary>
    /// Returns SKU detail enriched with gallery images from Media.API.
    /// Used for SKU-specific detail pages (variant selection, etc).
    /// </summary>
    public async Task<object?> GetSkuWithGalleryAsync(
        Guid skuId,
        CancellationToken ct = default)
    {
        return await FetchWithGalleryAsync(
            $"/api/catalog/products/skus/{skuId}",
            "SKU",
            skuId,
            ct);
    }

    /// <summary>
    /// Returns gallery for a specific SKU (lightweight, no SKU metadata).
    /// </summary>
    public async Task<List<GalleryItemDto>> GetSkuGalleryAsync(
        Guid skuId,
        CancellationToken ct = default)
    {
        try
        {
            var mediaClient = httpClientFactory.CreateClient("media-api");
            var response = await mediaClient.GetAsync(
                $"/api/media/gallery/SKU/{skuId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var gallery = await response.Content.ReadFromJsonAsync<List<GalleryItemDto>>(cancellationToken: ct) ?? [];
                return ResolveGalleryUrls(gallery);
            }
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch gallery for SKU {SkuId}", skuId);
            return [];
        }
    }

    /// <summary>
    /// Shared pattern: fetch catalog entity + gallery in parallel, merge gallery into response.
    /// </summary>
    private async Task<object?> FetchWithGalleryAsync(
        string catalogPath,
        string targetType,
        Guid targetId,
        CancellationToken ct)
    {
        var catalogClient = httpClientFactory.CreateClient("catalog-api");
        var mediaClient = httpClientFactory.CreateClient("media-api");

        // Parallel fetch: catalog entity + media gallery simultaneously
        var catalogTask = catalogClient.GetAsync(catalogPath, ct);
        var galleryTask = mediaClient.GetAsync($"/api/media/gallery/{targetType}/{targetId}", ct);

        await Task.WhenAll(catalogTask, galleryTask);

        // Process catalog response
        var catalogResponse = await catalogTask;
        if (!catalogResponse.IsSuccessStatusCode) return null;

        var entity = await catalogResponse.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        if (entity is null) return null;

        // Process gallery response (non-fatal if fails)
        var gallery = await SafeReadGalleryAsync(galleryTask, targetType, targetId, ct);

        // Merge: add gallery to entity response with absolute URLs
        return MergeGalleryIntoResponse(entity, gallery, mediaClient);
    }

    private async Task<List<GalleryItemDto>?> SafeReadGalleryAsync(
        Task<HttpResponseMessage> galleryTask,
        string targetType,
        Guid targetId,
        CancellationToken ct)
    {
        try
        {
            var galleryResponse = await galleryTask;
            if (galleryResponse.IsSuccessStatusCode)
                return await galleryResponse.Content.ReadFromJsonAsync<List<GalleryItemDto>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch gallery for {TargetType} {TargetId}", targetType, targetId);
        }
        return null;
    }

    private static List<GalleryItemDto> ResolveGalleryUrls(
        List<GalleryItemDto> gallery)
    {
        // URLs from Media.API are already relative (/api/media/{id}).
        // Do NOT prepend the internal Docker hostname — the browser resolves
        // relative URLs against the gateway origin (localhost:5293).
        return gallery;
    }

    private object? MergeGalleryIntoResponse(
        object entity,
        List<GalleryItemDto>? gallery,
        HttpClient mediaClient)
    {
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(
            System.Text.Json.JsonSerializer.Serialize(entity));

        if (dict is not null)
        {
            var resolvedGallery = ResolveGalleryUrls(gallery ?? []);

            // Fallback: if gallery is empty but entity has imageUrl, create a synthetic entry
            if (resolvedGallery.Count == 0 && dict.TryGetValue("imageUrl", out var imgUrlObj))
            {
                var imgUrl = imgUrlObj?.ToString();
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    // imageUrl is already relative (/api/media/{id}) — use as-is
                    resolvedGallery = [new GalleryItemDto(
                        Guid.Empty, "primary", "image/jpeg", imgUrl, null, 0, "Image", 0, true, DateTime.UtcNow)];
                }
            }

            dict["gallery"] = resolvedGallery;
        }

        return dict;
    }
}

public sealed record GalleryItemDto(
    Guid Id,
    string FileName,
    string ContentType,
    string Url,
    string? ThumbnailUrl,
    long SizeBytes,
    string Type,
    int SortOrder,
    bool IsPrimary,
    DateTime CreatedAt);
