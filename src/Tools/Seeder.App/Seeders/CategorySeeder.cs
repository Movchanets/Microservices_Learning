using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

public class CategorySeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public CategorySeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CategoryDto?> EnsureCategoryExistsAsync(CategoryModel category, string adminToken, List<CategoryDto> existingCategories, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var existing = existingCategories.FirstOrDefault(c => c.Name == category.Name);
        if (existing != null)
        {
            _logger.LogInformation("Category already exists: {Name}", category.Name);
            return existing;
        }

        var response = await _client.PostAsJsonAsync("/api/catalog/categories", category, ct);
        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: ct);
            _logger.LogInformation("Created category: {Name}", category.Name);
            return dto;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to create category {Name}: {StatusCode} - {Error}", category.Name, response.StatusCode, error);
            return null;
        }
    }

    public async Task<List<CategoryDto>> GetExistingCategoriesAsync(CancellationToken ct)
    {
        var response = await _client.GetAsync("/api/catalog/categories", ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<CategoryDto>>(cancellationToken: ct) ?? new List<CategoryDto>();
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Failed to fetch existing categories: {StatusCode} - {Error}", response.StatusCode, error);
        return new List<CategoryDto>();
    }
}