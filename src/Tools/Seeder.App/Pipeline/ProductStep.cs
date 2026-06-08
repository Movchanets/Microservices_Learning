using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 5: Create products and their variant SKUs via Catalog API.
/// Returns a lookup: skuCode → (storeId, productId, skuId).
/// </summary>
public class ProductStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly SellerRegistry _sellers;

    public ProductStep(
        HttpClient client, ILogger logger, SellerRegistry sellers)
    {
        _client = client;
        _logger = logger;
        _sellers = sellers;
    }

    public async Task<Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)>> ExecuteAsync(
        CatalogDataModel catalogData,
        Dictionary<string, Guid> categoryMapping,
        CancellationToken ct)
    {
        var productSeeder = new ProductSeeder(_client, _logger);
        var productIds = new Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)>();
        var successCount = 0;
        var skipCount = 0;

        var variantsByProduct = catalogData.ProductVariants
            .GroupBy(v => v.ProductExternalId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var product in catalogData.BaseProducts)
        {
            var sellerCtx = _sellers.ResolveByStoreName(product.StoreName);
            if (sellerCtx == null)
            {
                _logger.LogWarning(
                    "Skipping product {Name} — no seller context for store '{Store}'.",
                    product.Title, product.StoreName);
                skipCount++;
                continue;
            }

            if (!categoryMapping.TryGetValue(product.CategoryId, out var categoryId))
            {
                _logger.LogWarning(
                    "Skipping product {Name} — missing category.", product.Title);
                skipCount++;
                continue;
            }

            variantsByProduct.TryGetValue(product.ExternalId, out var variants);

            var result = await productSeeder.EnsureProductExistsAsync(
                product, variants ?? new List<ScrapedProductVariant>(), sellerCtx.Token, categoryId, sellerCtx.StoreId, ct);
            if (result != null)
            {
                var (productId, skuIds) = result.Value;
                foreach (var (skuCode, skuId) in skuIds)
                    productIds[skuCode] = (sellerCtx.StoreId, productId, skuId);
                successCount++;
            }

            await Task.Delay(500, ct);
        }

        _logger.LogInformation(
            "Products seeded: {Success} created, {Skipped} skipped, {Total} total SKUs",
            successCount, skipCount, productIds.Count);

        return productIds;
    }
}
