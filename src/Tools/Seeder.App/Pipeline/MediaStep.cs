using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 7: Upload gallery images for all products and their variant SKUs.
/// Non-fatal — failures are logged and skipped.
/// </summary>
public class MediaStep
{
    private readonly HttpClient _gatewayClient;
    private readonly HttpClient _mediaClient;
    private readonly HttpClient _downloadClient;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;
    private readonly SellerRegistry _sellers;

    public MediaStep(
        HttpClient gatewayClient,
        HttpClient mediaClient,
        HttpClient downloadClient,
        ILogger logger,
        string dataDirectory,
        SellerRegistry sellers)
    {
        _gatewayClient = gatewayClient;
        _mediaClient = mediaClient;
        _downloadClient = downloadClient;
        _logger = logger;
        _dataDirectory = dataDirectory;
        _sellers = sellers;
    }

    public async Task ExecuteAsync(
        CatalogDataModel catalogData,
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        CancellationToken ct)
    {
        var mediaSeeder = new MediaSeeder(
            _gatewayClient, _mediaClient, _downloadClient, _logger, _dataDirectory);
        var skuIdLookup = productIds.ToDictionary(k => k.Key, v => v.Value.SkuId);
        var uploadCount = 0;
        var failCount = 0;

        var variantsByProduct = catalogData.ProductVariants
            .GroupBy(v => v.ProductExternalId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var product in catalogData.BaseProducts)
        {
            variantsByProduct.TryGetValue(product.ExternalId, out var variants);
            var primaryVariant = variants?.FirstOrDefault();
            if (primaryVariant == null) continue;

            var primarySku = ProductSeedData.NormalizeSku(primaryVariant.Sku);
            if (!productIds.TryGetValue(primarySku, out var ids))
                continue;

            try
            {
                var sellerCtx = _sellers.ResolveByStoreName(product.StoreName);
                if (sellerCtx == null) continue;

                await mediaSeeder.UploadProductAndVariantGalleriesAsync(
                    ids.ProductId, skuIdLookup, product, variants!, sellerCtx.Token, ct);
                uploadCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Media upload failed for {Name} (non-fatal)", product.Title);
                failCount++;
            }

            await Task.Delay(500, ct);
        }

        _logger.LogInformation(
            "Media upload: {Success} succeeded, {Failed} failed", uploadCount, failCount);
    }
}
