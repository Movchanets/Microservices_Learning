using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

/// <summary>
/// Creates products and their variant SKUs via Catalog API.
/// Idempotent — skips creation if product already exists (matched by SKU code).
///
/// Flow:
///   1. Check if product exists by primary SKU code
///   2. If exists → return existing product ID + SKU IDs
///   3. If not → create product, create primary SKU, create variant SKUs, activate
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

    /// <summary>
    /// Ensures a product exists in the Catalog API. Creates it if missing.
    /// Returns (ProductId, skuCode→skuId mapping) or null on failure.
    /// </summary>
    public async Task<(Guid ProductId, Dictionary<string, Guid> SkuIds)?> EnsureProductExistsAsync(
        ProductModel product, string token, Guid categoryId, Guid storeId, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var primarySku = ProductSeedData.ResolvePrimarySku(product);
        if (string.IsNullOrWhiteSpace(primarySku))
        {
            _logger.LogWarning("Skipping product {Name} — no SKU found in product or variants.", product.Name);
            return null;
        }

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

        // ── Create product ───────────────────────────────────────
        var request = new
        {
            product.Name,
            product.Description,
            CategoryId = categoryId,
            StoreId = storeId,
            product.Tags,
            ImageUrl = ProductSeedData.ResolvePrimaryImage(product)
        };

        var response = await _client.PostAsJsonAsync("/api/catalog/products", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to create product {Name}: {StatusCode} - {Error}",
                product.Name, response.StatusCode, error);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);
        if (dto is null)
        {
            _logger.LogWarning("Failed to parse created product response for {Name}", product.Name);
            return null;
        }

        var skuIds2 = new Dictionary<string, Guid>();

        // Combine common attributes with variant attributes when creating SKUs,
        // since Catalog API currently expects all attributes at the SKU level via AddSkuCommand.
        var commonAttrs = product.CommonAttributes ?? new Dictionary<string, string>();

        // ── Create variant SKUs ──────────────────────────────────
        if (product.Variants != null && product.Variants.Count > 0)
        {
            foreach (var variant in product.Variants)
            {
                var variantSkuCode = ProductSeedData.NormalizeSku(variant.Sku);
                if (skuIds2.ContainsKey(variantSkuCode)) continue;

                var variantAttrs = new Dictionary<string, string>(commonAttrs);
                if (variant.Attributes != null)
                {
                    foreach (var kvp in variant.Attributes)
                    {
                        variantAttrs[kvp.Key] = kvp.Value;
                    }
                }

                var variantSkuId = await CreateSkuAsync(
                    dto.Id, variantSkuCode, variant.Price, product.Currency,
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
        }
        else
        {
            // Create a default primary SKU if no variants exist
            var primarySkuId = await CreateSkuAsync(
                dto.Id, primarySku, product.Price, product.Currency, commonAttrs, ct);
            if (primarySkuId != null)
                skuIds2[primarySku] = primarySkuId.Value;
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
                    product.Name, activateResponse.StatusCode, activateError);
            }
        }
        else
        {
            _logger.LogWarning("Skipping activation for {Name} — no SKUs created.", product.Name);
        }

        _logger.LogInformation("Created product: {Name} with {Count} SKUs",
            product.Name, skuIds2.Count);
        return (dto.Id, skuIds2);
    }

    /// <summary>
    /// Creates a single SKU on a product via Catalog API.
    /// Returns the SKU ID or null on failure.
    /// </summary>
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

    /// <summary>
    /// Looks up a store ID by name from the StoreManagement API.
    /// </summary>
    public async Task<Guid?> GetStoreIdAsync(string storeName, CancellationToken ct)
    {
        var response = await _client.GetAsync("/api/stores", ct);
        if (response.IsSuccessStatusCode)
        {
            var storesResponse = await response.Content.ReadFromJsonAsync<List<StoreDto>>(
                cancellationToken: ct);
            return storesResponse?.FirstOrDefault(s => s.Name == storeName)?.Id;
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Failed to fetch stores: {StatusCode} - {Error}",
            response.StatusCode, error);
        return null;
    }
}
