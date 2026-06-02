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
        List<ProductModel> products,
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        CancellationToken ct)
    {
        var inventorySeeder = new InventorySeeder(_client, _logger);

        foreach (var product in products)
        {
            var sellerCtx = _sellers.ResolveByStoreName(product.StoreName);
            if (sellerCtx == null) continue;

            // Stock primary SKU
            if (productIds.TryGetValue(product.Sku, out var ids))
            {
                await inventorySeeder.EnsureInventoryStockedAsync(
                    product, sellerCtx.Token, ids.StoreId, ids.ProductId, ct);
            }

            // Stock variant SKUs
            if (product.Variants == null) continue;

            foreach (var variant in product.Variants)
            {
                var variantSku = $"ROZ-{variant.RozetkaCode}";
                if (productIds.TryGetValue(variantSku, out var variantIds))
                {
                    await inventorySeeder.EnsureInventoryStockedAsync(
                        product with { Sku = variantSku },
                        sellerCtx.Token, variantIds.StoreId, variantIds.ProductId, ct);
                }
            }
        }
    }
}
