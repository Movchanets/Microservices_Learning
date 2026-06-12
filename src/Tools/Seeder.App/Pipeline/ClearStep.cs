using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Pipeline;

/// <summary>
/// Clear Step: Wipes all products and categories from the Catalog API.
/// Run with --clear flag to start with a clean slate before re-seeding.
/// </summary>
public class ClearStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public ClearStep(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task ExecuteAsync(string adminToken, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // ── Delete all products ────────────────────────────────
        await DeleteAllProductsAsync(ct);

        // ── Delete all categories ──────────────────────────────
        await DeleteAllCategoriesAsync(ct);

        _logger.LogInformation("Clear step completed — database is clean.");
    }

    private async Task DeleteAllProductsAsync(CancellationToken ct)
    {
        try
        {
            var response = await _client.GetAsync("/api/catalog/products", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to list products for clearing: {StatusCode}", response.StatusCode);
                return;
            }

            var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(cancellationToken: ct);
            if (products == null || products.Count == 0)
            {
                _logger.LogInformation("No products to clear.");
                return;
            }

            _logger.LogInformation("Deleting {Count} products...", products.Count);
            var deleted = 0;
            foreach (var product in products)
            {
                try
                {
                    var deleteResponse = await _client.DeleteAsync($"/api/catalog/products/{product.Id}", ct);
                    if (deleteResponse.IsSuccessStatusCode)
                        deleted++;
                    else
                        _logger.LogWarning("Failed to delete product {Id}: {StatusCode}", product.Id, deleteResponse.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting product {Id}", product.Id);
                }
            }
            _logger.LogInformation("Deleted {Deleted}/{Total} products.", deleted, products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear products.");
        }
    }

    private async Task DeleteAllCategoriesAsync(CancellationToken ct)
    {
        try
        {
            var response = await _client.GetAsync("/api/catalog/categories", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to list categories for clearing: {StatusCode}", response.StatusCode);
                return;
            }

            var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(cancellationToken: ct);
            if (categories == null || categories.Count == 0)
            {
                _logger.LogInformation("No categories to clear.");
                return;
            }

            // Delete leaf categories first (children before parents)
            // Sort by name descending as a heuristic — child categories tend to have longer/more specific names
            var sorted = categories.OrderByDescending(c => c.Name).ToList();

            _logger.LogInformation("Deleting {Count} categories...", sorted.Count);
            var deleted = 0;
            foreach (var category in sorted)
            {
                try
                {
                    var deleteResponse = await _client.DeleteAsync($"/api/catalog/categories/{category.Id}", ct);
                    if (deleteResponse.IsSuccessStatusCode)
                        deleted++;
                    else
                        _logger.LogWarning("Failed to delete category {Name} ({Id}): {StatusCode}",
                            category.Name, category.Id, deleteResponse.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting category {Name} ({Id})", category.Name, category.Id);
                }
            }
            _logger.LogInformation("Deleted {Deleted}/{Total} categories.", deleted, sorted.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear categories.");
        }
    }
}
