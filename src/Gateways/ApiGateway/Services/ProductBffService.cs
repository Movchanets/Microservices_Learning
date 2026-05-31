using System.Net.Http.Headers;

namespace ApiGateway.Services;

/// <summary>
/// BFF aggregation service: fetches product/SKU from catalog-api, enriches with gallery from media-api.
/// Uses parallel fetches to minimize latency (hybrid caching pattern).
///
/// Architecture:
///   List views → Product.ImageUrl cached in Catalog.DB (no Media.API call)
///   Detail pages → BFF fetches catalog + gallery in parallel, merges into single response
/// </summary>
public sealed class ProductBffService(
    IHttpClientFactory httpClientFactory,
    ILogger<ProductBffService> logger)
{
    // ── Public API ───────────────────────────────────────────────

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
    /// Used when only gallery images are needed (e.g., thumbnail strip).
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

            if (!response.IsSuccessStatusCode)
                return [];

            var gallery = await response.Content.ReadFromJsonAsync<List<GalleryItemDto>>(
                cancellationToken: ct) ?? [];

            return ResolveGalleryUrls(gallery);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch gallery for SKU {SkuId}", skuId);
            return [];
        }
    }

    // ── Core Aggregation ─────────────────────────────────────────

    /// <summary>
    /// Shared pattern: fetch catalog entity + gallery in parallel, merge gallery into response.
    ///
    /// Flow:
    ///   1. Fire catalog-api and media-api requests simultaneously (Task.WhenAll)
    ///   2. Deserialize catalog entity — return null if not found
    ///   3. Safely read gallery response (non-fatal if fails)
    ///   4. Merge gallery array into entity response with resolved URLs
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
        var galleryTask = mediaClient.GetAsync(
            $"/api/media/gallery/{targetType}/{targetId}", ct);

        await Task.WhenAll(catalogTask, galleryTask);

        // Process catalog response (fatal — return null if not found)
        var catalogResponse = await catalogTask;
        if (!catalogResponse.IsSuccessStatusCode)
            return null;

        var entity = await catalogResponse.Content.ReadFromJsonAsync<object>(
            cancellationToken: ct);
        if (entity is null)
            return null;

        // Process gallery response (non-fatal — graceful degradation)
        var gallery = await SafeReadGalleryAsync(galleryTask, targetType, targetId, ct);

        // Merge: add gallery array to entity response with absolute URLs
        return MergeGalleryIntoResponse(entity, gallery, mediaClient);
    }

    // ── Gallery Helpers ──────────────────────────────────────────

    /// <summary>
    /// Safely reads gallery response — returns null on failure instead of throwing.
    /// Gallery failures are non-fatal: the product detail page can still render
    /// with the fallback Product.ImageUrl.
    /// </summary>
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
                return await galleryResponse.Content.ReadFromJsonAsync<List<GalleryItemDto>>(
                    cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to fetch gallery for {TargetType} {TargetId}", targetType, targetId);
        }
        return null;
    }

    /// <summary>
    /// Resolves gallery URLs. Currently a no-op — URLs from Media.API are already
    /// relative (/api/media/{id}). The browser resolves them against the gateway origin.
    /// Do NOT prepend the internal Docker hostname — it breaks CORS.
    /// </summary>
    private static List<GalleryItemDto> ResolveGalleryUrls(
        List<GalleryItemDto> gallery)
    {
        return gallery;
    }

    /// <summary>
    /// Merges gallery array into the catalog entity response.
    /// Serializes entity to Dictionary, adds "gallery" key, returns as object.
    ///
    /// Fallback: if gallery is empty but entity has imageUrl, creates a synthetic
    /// gallery entry so the frontend always has at least one image to display.
    /// </summary>
    private object? MergeGalleryIntoResponse(
        object entity,
        List<GalleryItemDto>? gallery,
        HttpClient mediaClient)
    {
        var dict = System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, object?>>(
                System.Text.Json.JsonSerializer.Serialize(entity));

        if (dict is not null)
        {
            var resolvedGallery = ResolveGalleryUrls(gallery ?? []);

            // Fallback: if gallery is empty but entity has imageUrl, create synthetic entry
            if (resolvedGallery.Count == 0
                && dict.TryGetValue("imageUrl", out var imgUrlObj))
            {
                var imgUrl = imgUrlObj?.ToString();
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    resolvedGallery =
                    [
                        new GalleryItemDto(
                            Guid.Empty, "primary", "image/jpeg",
                            imgUrl, null, 0, "Image", 0, true, DateTime.UtcNow)
                    ];
                }
            }

            dict["gallery"] = resolvedGallery;
        }

        return dict;
    }
}

/// <summary>
/// Gallery item returned from Media.API. Used by BFF to merge into product/SKU responses.
/// </summary>
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
