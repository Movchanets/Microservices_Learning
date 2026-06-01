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

    /// <summary>
    /// Fetches existing attribute definitions for a category.
    /// </summary>
    public async Task<List<AttributeDefinitionResponse>> GetAttributeDefinitionsAsync(
        Guid categoryId, CancellationToken ct)
    {
        var response = await _client.GetAsync(
            $"/api/catalog/categories/{categoryId}/attributes", ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<AttributeDefinitionResponse>>(
                       cancellationToken: ct) ?? [];
        }
        return [];
    }

    /// <summary>
    /// Ensures an attribute definition exists on a category.
    /// Creates it if missing (matched by Key).
    /// </summary>
    public async Task EnsureAttributeDefinitionAsync(
        Guid categoryId,
        AttributeDefinitionModel attr,
        string adminToken,
        CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // Check if already exists
        var existing = await GetAttributeDefinitionsAsync(categoryId, ct);
        if (existing.Any(a => a.Key.Equals(attr.Key, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("Attribute definition already exists: {Key} on category {CategoryId}",
                attr.Key, categoryId);
            return;
        }

        var request = new
        {
            attr.Key,
            attr.DisplayName,
            attr.Target,
            attr.ValueType,
            attr.IsFilterable,
            attr.IsRequired,
            attr.SortOrder,
            attr.AllowedValues,
            attr.IsVariantAxis
        };

        _logger.LogInformation(
            "POST /api/catalog/categories/{CategoryId}/attributes — Key='{Key}', IsVariantAxis={IsVariantAxis}, AllowedValues=[{Values}]",
            categoryId, attr.Key, attr.IsVariantAxis,
            attr.AllowedValues != null ? string.Join(", ", attr.AllowedValues) : "null");

        var response = await _client.PostAsJsonAsync(
            $"/api/catalog/categories/{categoryId}/attributes", request, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Created attribute definition: {Key} (variantAxis={IsVariantAxis}) on category {CategoryId}",
                attr.Key, attr.IsVariantAxis, categoryId);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to create attribute definition {Key} on category {CategoryId}: {StatusCode} - {Error}",
                attr.Key, categoryId, response.StatusCode, error);
        }
    }
}

/// <summary>Attribute definition response from Catalog API.</summary>
public record AttributeDefinitionResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsVariantAxis { get; set; }
}
