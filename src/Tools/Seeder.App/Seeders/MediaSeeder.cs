using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

public class MediaSeeder
{
    private readonly HttpClient _client;
    private readonly HttpClient _mediaClient;
    private readonly HttpClient _downloadClient;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;

    public MediaSeeder(HttpClient client, HttpClient mediaClient, HttpClient downloadClient, ILogger logger, string dataDirectory)
    {
        _client = client;
        _mediaClient = mediaClient;
        _downloadClient = downloadClient;
        _logger = logger;
        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// Upload all gallery images for a product.
    /// First image is set as primary.
    /// Supports both local file paths and HTTP(S) URLs.
    /// </summary>
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
            byte[]? fileBytes = null;
            string fileName;

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Download from URL
                try
                {
                    fileBytes = await _downloadClient.GetByteArrayAsync(path, ct);
                    fileName = Path.GetFileName(new Uri(path).AbsolutePath);
                    if (string.IsNullOrEmpty(fileName))
                        fileName = $"image{i}.jpg";
                    _logger.LogInformation("  ⬇️ Downloaded {Url} ({Size} bytes)", path, fileBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "  ⚠️ Failed to download {Url}", path);
                    continue;
                }
            }
            else
            {
                // Local file
                var localPath = Path.Combine(_dataDirectory, path);
                if (!File.Exists(localPath))
                {
                    _logger.LogWarning("Image not found: {Path}", localPath);
                    continue;
                }
                fileBytes = await File.ReadAllBytesAsync(localPath, ct);
                fileName = Path.GetFileName(localPath);
            }

            if (fileBytes is null || fileBytes.Length == 0)
                continue;

            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    GetContentType(fileName, fileBytes));
                content.Add(fileContent, "file", fileName);
                content.Add(new StringContent(productId.ToString()), "targetId");
                content.Add(new StringContent(targetType), "targetType");
                content.Add(new StringContent((i == 0).ToString().ToLower()), "isPrimary");

                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/media/upload")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _mediaClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var idStr = System.Text.Json.JsonDocument.Parse(json)
                        .RootElement.GetProperty("id").GetString();
                    if (Guid.TryParse(idStr, out var mediaId))
                    {
                        mediaIds.Add(mediaId);
                        _logger.LogInformation("  📸 Uploaded {File} -> {MediaId}", fileName, mediaId);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("  ⚠️ Upload failed {File}: {Status} - {Error}",
                        fileName, response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "  ⚠️ Upload error for {File}", fileName);
            }

            // Small delay between uploads
            await Task.Delay(200, ct);
        }

        return mediaIds;
    }

    private static string GetContentType(string fileName, byte[] fileBytes)
    {
        // Detect by magic bytes
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
        // Fallback to extension
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

    /// <summary>
    /// Upload gallery for a product and all its variant SKUs.
    /// </summary>
    public async Task UploadProductAndVariantGalleriesAsync(
        Guid productId,
        Dictionary<string, Guid> skuIds,
        ProductModel product,
        string token,
        CancellationToken ct = default)
    {
        // Upload main product gallery
        _logger.LogInformation("Gallery check for {Name}: Gallery={GalleryCount}, ImageUrl={ImageUrl}",
            product.Name, product.Gallery?.Count ?? 0, product.ImageUrl ?? "(null)");

        if (product.Gallery?.Count > 0)
        {
            _logger.LogInformation("Uploading {Count} images for product {Name} (targetId={ProductId})",
                product.Gallery.Count, product.Name, productId);
            await UploadProductGalleryAsync(productId, product.Gallery, token, "Product", ct);
        }
        else if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            // Single image fallback
            _logger.LogInformation("Uploading single image for product {Name} (targetId={ProductId})",
                product.Name, productId);
            await UploadProductGalleryAsync(productId, [product.ImageUrl], token, "Product", ct);
        }

        // Upload variant SKU galleries
        if (product.Variants != null)
        {
            foreach (var variant in product.Variants)
            {
                var variantSku = $"ROZ-{variant.RozetkaCode}";
                if (!skuIds.TryGetValue(variantSku, out var skuId))
                    continue;

                var variantImages = variant.Gallery ?? new List<string>();
                if (variantImages.Count == 0 && !string.IsNullOrEmpty(variant.ImageUrl))
                    variantImages = [variant.ImageUrl];

                if (variantImages.Count > 0)
                {
                    _logger.LogInformation("  Uploading {Count} images for variant {Name}", variantImages.Count, variant.Name);
                    await UploadProductGalleryAsync(skuId, variantImages, token, "SKU", ct);
                }
            }
        }
    }
}
