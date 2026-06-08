using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

/// <summary>
/// Uploads product and SKU gallery images to Media.API.
/// Supports both local file paths and HTTP(S) URLs.
///
/// Image source resolution:
///   1. If path starts with http:// or https:// → download from URL
///   2. Otherwise → read from local Data/Images/ directory
///
/// Content type detection:
///   1. Magic bytes (WebP, PNG, JPEG, GIF) — most reliable
///   2. File extension fallback — less reliable (e.g., .jpg files that are actually WebP)
///
/// First image in each gallery is set as primary (thumbnail).
/// </summary>
public class MediaSeeder
{
    private readonly HttpClient _client;
    private readonly HttpClient _mediaClient;
    private readonly HttpClient _downloadClient;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;

    public MediaSeeder(
        HttpClient client,
        HttpClient mediaClient,
        HttpClient downloadClient,
        ILogger logger,
        string dataDirectory)
    {
        _client = client;
        _mediaClient = mediaClient;
        _downloadClient = downloadClient;
        _logger = logger;
        _dataDirectory = dataDirectory;
    }

    // ── Public API ──────────────────────────────────────────────

    /// <summary>
    /// Upload all gallery images for a product or SKU.
    /// First image is automatically set as primary.
    /// Supports both local file paths and HTTP(S) URLs.
    /// </summary>
    /// <param name="productId">Target entity ID (Product or SKU).</param>
    /// <param name="imagePaths">Image paths (local relative or HTTP URLs).</param>
    /// <param name="token">Auth token for Media API.</param>
    /// <param name="targetType">"Product" or "SKU".</param>
    /// <returns>List of created media item IDs.</returns>
    public async Task<List<Guid>> UploadProductGalleryAsync(
        Guid productId,
        List<string> imagePaths,
        string token,
        string targetType = "Product",
        CancellationToken ct = default)
    {
        var mediaIds = new List<Guid>();

        for (int i = 0; i < imagePaths.Count; i++)
        {
            var path = imagePaths[i];
            var fileBytes = await LoadImageBytesAsync(path, i);
            if (fileBytes is null || fileBytes.Length == 0)
                continue;

            var fileName = ResolveFileName(path, i);
            var mediaId = await UploadSingleImageAsync(
                productId, fileBytes, fileName, targetType,
                isPrimary: i == 0, token, ct);

            if (mediaId.HasValue)
                mediaIds.Add(mediaId.Value);

            // Small delay between uploads to avoid overwhelming the API
            await Task.Delay(200, ct);
        }

        return mediaIds;
    }

    /// <summary>
    /// Upload gallery for a product and all its variant SKUs.
    /// Handles both gallery arrays and single ImageUrl fallback.
    /// </summary>
    public async Task UploadProductAndVariantGalleriesAsync(
        Guid productId,
        Dictionary<string, Guid> skuIds,
        ScrapedBaseProduct product,
        List<ScrapedProductVariant> variants,
        string token,
        CancellationToken ct = default)
    {
        // ── Upload variant SKU galleries ─────────────────────────
        if (variants.Count > 0)
        {
            foreach (var variant in variants)
            {
                var variantSku = ProductSeedData.NormalizeSku(variant.Sku);
                if (!skuIds.TryGetValue(variantSku, out var skuId))
                    continue;

                var variantImages = variant.Images ?? [];
                
                if (variantImages.Count > 0)
                {
                    _logger.LogInformation(
                        "  Uploading {Count} images for variant SKU {Sku}",
                        variantImages.Count, variantSku);
                    // Pass TargetType = "SKU" to hit the Media API upload endpoint.
                    // Media.API uses TargetId for SkuId when TargetType is "SKU"
                    await UploadProductGalleryAsync(skuId, variantImages, token, "SKU", ct);
                }
            }
        }
        else
        {
            _logger.LogWarning("No variants found for product {Name} (targetId={ProductId}) to upload images.", product.Title, productId);
        }
    }

    // ── Image Loading ────────────────────────────────────────────

    /// <summary>
    /// Loads image bytes from either a URL or local file path.
    /// Returns null if the image cannot be loaded (non-fatal).
    /// </summary>
    private async Task<byte[]?> LoadImageBytesAsync(string path, int index)
    {
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return await DownloadFromUrlAsync(path);
        }

        return await LoadFromLocalFileAsync(path);
    }

    private async Task<byte[]?> DownloadFromUrlAsync(string url)
    {
        try
        {
            var fileBytes = await _downloadClient.GetByteArrayAsync(url);
            _logger.LogInformation(
                "  ⬇️ Downloaded {Url} ({Size} bytes)", url, fileBytes.Length);
            return fileBytes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "  ⚠️ Failed to download {Url}", url);
            return null;
        }
    }

    private async Task<byte[]?> LoadFromLocalFileAsync(string relativePath)
    {
        var localPath = Path.Combine(_dataDirectory, relativePath);
        if (!File.Exists(localPath))
        {
            _logger.LogWarning("Image not found: {Path}", localPath);
            return null;
        }
        return await File.ReadAllBytesAsync(localPath);
    }

    private static string ResolveFileName(string path, int index)
    {
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileName(new Uri(path).AbsolutePath);
            return string.IsNullOrEmpty(fileName) ? $"image{index}.jpg" : fileName;
        }
        return Path.GetFileName(path);
    }

    // ── Upload ───────────────────────────────────────────────────

    /// <summary>
    /// Uploads a single image to Media.API via multipart/form-data.
    /// </summary>
    private async Task<Guid?> UploadSingleImageAsync(
        Guid targetId,
        byte[] fileBytes,
        string fileName,
        string targetType,
        bool isPrimary,
        string token,
        CancellationToken ct)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                GetContentType(fileName, fileBytes));
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(targetId.ToString()), "targetId");
            content.Add(new StringContent(targetType), "targetType");
            content.Add(new StringContent(isPrimary.ToString().ToLower()), "isPrimary");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/media/upload")
            {
                Content = content
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _mediaClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var idStr = System.Text.Json.JsonDocument.Parse(json)
                    .RootElement.GetProperty("id").GetString();
                if (Guid.TryParse(idStr, out var mediaId))
                {
                    _logger.LogInformation(
                        "  📸 Uploaded {File} -> {MediaId}", fileName, mediaId);
                    return mediaId;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "  ⚠️ Upload failed {File}: {Status} - {Error}",
                    fileName, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "  ⚠️ Upload error for {File}", fileName);
        }
        return null;
    }

    // ── Content Type Detection ───────────────────────────────────

    /// <summary>
    /// Detects content type from magic bytes first, then falls back to file extension.
    /// Magic bytes are more reliable — some Rozetka images are WebP but named .jpg.
    /// </summary>
    private static string GetContentType(string fileName, byte[] fileBytes)
    {
        // ── Magic byte detection (most reliable) ─────────────────
        if (fileBytes.Length >= 4)
        {
            // WebP: RIFF....WEBP
            if (fileBytes[0] == 0x52 && fileBytes[1] == 0x49 &&
                fileBytes[2] == 0x46 && fileBytes[3] == 0x46)
                return "image/webp";

            // PNG: \x89PNG
            if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 &&
                fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
                return "image/png";

            // JPEG: \xFF\xD8\xFF
            if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
                return "image/jpeg";

            // GIF: GIF8
            if (fileBytes[0] == 0x47 && fileBytes[1] == 0x49 &&
                fileBytes[2] == 0x46 && fileBytes[3] == 0x38)
                return "image/gif";
        }

        // ── Extension fallback (less reliable) ───────────────────
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            _ => "image/jpeg"
        };
    }
}
