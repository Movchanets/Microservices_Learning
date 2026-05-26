using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

public class ProductSeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public ProductSeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<(Guid ProductId, Guid SkuId)?> EnsureProductExistsAsync(ProductModel product, string token, Guid categoryId, Guid storeId, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Check if product exists by SKU
        var getResponse = await _client.GetAsync($"/api/catalog/products/sku/{product.Sku}", ct);
        if (getResponse.IsSuccessStatusCode)
        {
            var existing = await getResponse.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);
            _logger.LogInformation("Product already exists: {Sku}", product.Sku);
            // Need SkuId — fetch the full product to get it
            var fullProduct = await _client.GetAsync($"/api/catalog/products/{existing!.Id}", ct);
            if (fullProduct.IsSuccessStatusCode)
            {
                var full = await fullProduct.Content.ReadFromJsonAsync<ProductWithSkusDto>(cancellationToken: ct);
                var existingSku = full?.Skus?.FirstOrDefault(s => s.SkuCode == product.Sku);
                if (existingSku != null)
                    return (existing.Id, existingSku.Id);
            }
            // Fallback: return product ID with empty SkuId (shouldn't happen)
            return (existing!.Id, Guid.Empty);
        }

        var request = new
        {
            product.Name,
            product.Description,
            product.Price,
            product.Currency,
            product.Sku,
            CategoryId = categoryId,
            StoreId = storeId,
            product.Tags,
            product.ImageUrl
        };

        var response = await _client.PostAsJsonAsync("/api/catalog/products", request, ct);
        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);

            // Add SKU to the product (required before activation post-SKU refactor)
            var skuRequest = new
            {
                SkuCode = product.Sku,
                Price = product.Price,
                Currency = product.Currency,
                TypedAttributes = new Dictionary<string, string>(),
                FlexibleAttributes = (Dictionary<string, string>?)null
            };
            var skuResponse = await _client.PostAsJsonAsync($"/api/catalog/products/{dto!.Id}/skus", skuRequest, ct);
            Guid skuId = Guid.Empty;
            if (skuResponse.IsSuccessStatusCode)
            {
                var skuDto = await skuResponse.Content.ReadFromJsonAsync<SkuResponseDto>(cancellationToken: ct);
                skuId = skuDto?.Id ?? Guid.Empty;
                _logger.LogInformation("Added SKU {SkuCode} to product {Name}", product.Sku, product.Name);
            }
            else
            {
                var skuError = await skuResponse.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to add SKU for {Name}: {StatusCode} - {Error}", product.Name, skuResponse.StatusCode, skuError);
            }

            // Activate the product (requires at least one active SKU)
            try
            {
                await _client.PutAsync($"/api/catalog/products/{dto!.Id}/activate", null, ct);
            }
            catch { /* Ignore if endpoint doesn't exist */ }

            _logger.LogInformation("Created product: {Name}", product.Name);
            return (dto!.Id, skuId);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to create product {Name}: {StatusCode} - {Error}", product.Name, response.StatusCode, error);
            return null;
        }
    }

    public async Task<Guid?> GetStoreIdAsync(string storeName, CancellationToken ct)
    {
        var response = await _client.GetAsync("/api/stores", ct);
        if (response.IsSuccessStatusCode)
        {
            var storesResponse = await response.Content.ReadFromJsonAsync<List<StoreDto>>(cancellationToken: ct);
            return storesResponse?.FirstOrDefault(s => s.Name == storeName)?.Id;
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Failed to fetch stores in GetStoreIdAsync: {StatusCode} - {Error}", response.StatusCode, error);
        return null;
    }
}