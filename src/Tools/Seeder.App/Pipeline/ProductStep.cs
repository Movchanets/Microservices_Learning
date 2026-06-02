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
        List<ProductModel> products,
        List<CategoryDto> categories,
        Dictionary<string, string> categoryMapping,
        CancellationToken ct)
    {
        var productSeeder = new ProductSeeder(_client, _logger);
        var productIds = new Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)>();
        var successCount = 0;
        var skipCount = 0;

        foreach (var product in products)
        {
            var sellerCtx = _sellers.ResolveByStoreName(product.StoreName);
            if (sellerCtx == null)
            {
                _logger.LogWarning(
                    "Skipping product {Name} — no seller context for store '{Store}'.",
                    product.Name, product.StoreName);
                skipCount++;
                continue;
            }

            var categoryId = CategoryResolver.FindBest(
                product.CategoryName, categories, categoryMapping);
            if (categoryId == null)
            {
                _logger.LogWarning(
                    "Skipping product {Name} — missing category.", product.Name);
                skipCount++;
                continue;
            }

            var result = await productSeeder.EnsureProductExistsAsync(
                product, sellerCtx.Token, categoryId.Value, sellerCtx.StoreId, ct);
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
