using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 6: Stock initial inventory for all products and their variants.
/// </summary>
public class InventoryStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly SellerRegistry _sellers;

    public InventoryStep(HttpClient client, ILogger logger, SellerRegistry sellers)
    {
        _client = client;
        _logger = logger;
        _sellers = sellers;
    }

    public async Task ExecuteAsync(
        CatalogDataModel catalogData,
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        CancellationToken ct)
    {
        var inventorySeeder = new InventorySeeder(_client, _logger);

        var productStoreMap = catalogData.BaseProducts
            .ToDictionary(p => p.ExternalId, p => p.StoreName);

        foreach (var variant in catalogData.ProductVariants)
        {
            if (!productStoreMap.TryGetValue(variant.ProductExternalId, out var storeName))
                continue;

            var sellerCtx = _sellers.ResolveByStoreName(storeName);
            if (sellerCtx == null) continue;

            var variantSku = ProductSeedData.NormalizeSku(variant.Sku);
            if (productIds.TryGetValue(variantSku, out var variantIds))
            {
                await inventorySeeder.EnsureInventoryStockedAsync(
                    variantSku, variant.InitialStock, sellerCtx.Token, variantIds.StoreId, variantIds.ProductId, ct);
            }
        }
    }
}
