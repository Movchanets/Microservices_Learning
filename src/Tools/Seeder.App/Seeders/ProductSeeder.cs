using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

/// <summary>
/// Creates products and their variant SKUs via Catalog API.
/// Idempotent — skips creation if product already exists (matched by SKU code).
/// </summary>
public class ProductSeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public ProductSeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<(Guid ProductId, Dictionary<string, Guid> SkuIds)?> EnsureProductExistsAsync(
        ScrapedBaseProduct product, List<ScrapedProductVariant> variants, string token, Guid categoryId, Guid storeId, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var primaryVariant = variants.FirstOrDefault();
        if (primaryVariant == null)
        {
            _logger.LogWarning("Skipping product {Name} — no variants found.", product.Title);
            return null;
        }

        var primarySku = ProductSeedData.NormalizeSku(primaryVariant.Sku);

        // ── Check if product already exists ──────────────────────
        var getResponse = await _client.GetAsync($"/api/catalog/products/sku/{primarySku}", ct);
        if (getResponse.IsSuccessStatusCode)
        {
            var existing = await getResponse.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);
            _logger.LogInformation("Product already exists: {Sku}", primarySku);
            var fullProduct = await _client.GetAsync($"/api/catalog/products/{existing!.Id}", ct);
            if (fullProduct.IsSuccessStatusCode)
            {
                var full = await fullProduct.Content.ReadFromJsonAsync<ProductWithSkusDto>(cancellationToken: ct);
                var skuIds = new Dictionary<string, Guid>();
                foreach (var sku in full?.Skus ?? [])
                    skuIds[sku.SkuCode] = sku.Id;
                return (existing.Id, skuIds);
            }
            return (existing!.Id, new Dictionary<string, Guid>());
        }

        // ── Resolve Variant Axes ─────────────────────────────────
        List<Guid>? variantAxisIds = null;
        if (variants.Count > 0)
        {
            var categorySeeder = new CategorySeeder(_client, _logger);
            var attributes = await categorySeeder.GetAttributeDefinitionsAsync(categoryId, ct);
            
            // Assume any attribute that changes between variants is an axis, or just use all attributes from the first variant
            variantAxisIds = primaryVariant.Attributes.Keys
                .Select(k => attributes.FirstOrDefault(a => a.Key.Equals(k, StringComparison.OrdinalIgnoreCase))?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
        }

        // ── Create product ───────────────────────────────────────
        var request = new
        {
            Name = product.Title,
            Description = product.Description,
            CategoryId = categoryId,
            StoreId = storeId,
            Tags = new[] { product.Brand },
            ImageUrl = primaryVariant.Images.FirstOrDefault() ?? "",
            VariantAxisIds = variantAxisIds
        };

        var response = await _client.PostAsJsonAsync("/api/catalog/products", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to create product {Name}: {StatusCode} - {Error}",
                product.Title, response.StatusCode, error);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);
        if (dto is null)
        {
            _logger.LogWarning("Failed to parse created product response for {Name}", product.Title);
            return null;
        }

        var skuIds2 = new Dictionary<string, Guid>();

        // ── Create variant SKUs ──────────────────────────────────
        foreach (var variant in variants)
        {
            var variantSkuCode = ProductSeedData.NormalizeSku(variant.Sku);
            if (skuIds2.ContainsKey(variantSkuCode)) continue;

            var variantAttrs = new Dictionary<string, string>(variant.Attributes);
            // Append Brand
            variantAttrs["brand"] = product.Brand;

            var variantSkuId = await CreateSkuAsync(
                dto.Id, variantSkuCode, variant.Price, variant.Currency,
                variantAttrs, ct);
            if (variantSkuId != null)
            {
                skuIds2[variantSkuCode] = variantSkuId.Value;
                _logger.LogInformation("  + Variant SKU {SkuCode} attrs={Attrs}",
                    variantSkuCode, 
                    variantAttrs.Count > 0 
                        ? string.Join(", ", variantAttrs.Select(a => $"{a.Key}={a.Value}")) 
                        : "none");
            }
        }

        // ── Activate product (only if at least one SKU exists) ───
        if (skuIds2.Count > 0)
        {
            var activateResponse = await _client.PutAsync(
                $"/api/catalog/products/{dto.Id}/activate", null, ct);
            if (!activateResponse.IsSuccessStatusCode)
            {
                var activateError = await activateResponse.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to activate product {Name}: {StatusCode} - {Error}",
                    product.Title, activateResponse.StatusCode, activateError);
            }
        }
        else
        {
            _logger.LogWarning("Skipping activation for {Name} — no SKUs created.", product.Title);
        }

        _logger.LogInformation("Created product: {Name} with {Count} SKUs",
            product.Title, skuIds2.Count);
        return (dto.Id, skuIds2);
    }

    private async Task<Guid?> CreateSkuAsync(
        Guid productId, string skuCode, decimal price, string currency,
        Dictionary<string, string>? typedAttributes, CancellationToken ct)
    {
        var skuRequest = new
        {
            SkuCode = skuCode,
            Price = price,
            Currency = currency,
            TypedAttributes = typedAttributes ?? new Dictionary<string, string>(),
            FlexibleAttributes = (Dictionary<string, string>?)null
        };

        var skuResponse = await _client.PostAsJsonAsync(
            $"/api/catalog/products/{productId}/skus", skuRequest, ct);

        if (skuResponse.IsSuccessStatusCode)
        {
            var skuDto = await skuResponse.Content.ReadFromJsonAsync<SkuResponseDto>(
                cancellationToken: ct);
            return skuDto?.Id;
        }

        var skuError = await skuResponse.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Failed to add SKU {SkuCode}: {StatusCode} - {Error}",
            skuCode, skuResponse.StatusCode, skuError);
        return null;
    }
}
