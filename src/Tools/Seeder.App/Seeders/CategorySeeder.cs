using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

/// <summary>
/// Creates categories via Catalog API.
/// Idempotent — skips creation if category already exists (matched by name).
/// Supports parent→child hierarchy via ParentCategoryId.
/// </summary>
public class CategorySeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public CategorySeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Ensures a category exists. Creates it if missing.
    /// Checks against the provided existingCategories list to avoid duplicate API calls.
    /// </summary>
    /// <returns>The category DTO, or null on failure.</returns>
    public async Task<CategoryDto?> EnsureCategoryExistsAsync(
        CategoryModel category,
        string adminToken,
        List<CategoryDto> existingCategories,
        CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // Check in-memory list first (avoids unnecessary API calls)
        var existing = existingCategories.FirstOrDefault(c => c.Name == category.Name);
        if (existing != null)
        {
            _logger.LogInformation("Category already exists: {Name}", category.Name);
            return existing;
        }

        var request = new
        {
            category.Name,
            category.Description,
            ParentCategoryId = category.ParentCategoryId
        };

        var response = await _client.PostAsJsonAsync("/api/catalog/categories", request, ct);
        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<CategoryDto>(
                cancellationToken: ct);
            _logger.LogInformation("Created category: {Name} (parent={ParentId})",
                category.Name, category.ParentCategoryId);
            return dto;
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Failed to create category {Name}: {StatusCode} - {Error}",
            category.Name, response.StatusCode, error);
        return null;
    }

    /// <summary>
    /// Fetches all existing categories from the Catalog API.
    /// Used to populate the in-memory list for idempotency checks.
    /// </summary>
    public async Task<List<CategoryDto>> GetExistingCategoriesAsync(CancellationToken ct)
    {
        var response = await _client.GetAsync("/api/catalog/categories", ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<CategoryDto>>(
                       cancellationToken: ct) ?? new List<CategoryDto>();
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Failed to fetch existing categories: {StatusCode} - {Error}",
            response.StatusCode, error);
        return new List<CategoryDto>();
    }
}
