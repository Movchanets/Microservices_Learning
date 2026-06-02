using System.Text.Json;

namespace Seeder.App.Pipeline;

/// <summary>
/// Shared data loading and infrastructure utilities.
/// Used by all pipeline steps.
/// </summary>
public static class SeedDataLoader
{
    /// <summary>
    /// Loads and deserializes a JSON file from the Data directory.
    /// </summary>
    public static async Task<T> LoadJsonAsync<T>(string dataDirectory, string fileName)
    {
        var path = Path.Combine(dataDirectory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Seed data file not found: {path}");
        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }

    /// <summary>
    /// Loads category mapping from category-mapping.json (optional).
    /// Maps Rozetka category names to existing marketplace categories.
    /// </summary>
    public static async Task<Dictionary<string, string>> LoadCategoryMappingAsync(
        string dataDirectory, ILogger logger)
    {
        try
        {
            var mappingPath = Path.Combine(dataDirectory, "category-mapping.json");
            if (File.Exists(mappingPath))
            {
                var mappingJson = await File.ReadAllTextAsync(mappingPath);
                var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson)
                    ?? new();
                logger.LogInformation("Loaded {Count} category mappings", mapping.Count);
                return mapping;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load category mapping, using fallback matching");
        }
        return new();
    }

    /// <summary>
    /// Waits for the API Gateway to become responsive (up to 60 seconds).
    /// </summary>
    public static async Task WaitForGatewayAsync(HttpClient client, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("Waiting for API Gateway to become responsive...");
        var retries = 0;
        while (!ct.IsCancellationRequested && retries < 30)
        {
            try
            {
                var response = await client.GetAsync("/health", ct);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("API Gateway is ready.");
                    return;
                }
            }
            catch { }
            retries++;
            await Task.Delay(2000, ct);
        }
        throw new Exception("API Gateway did not become responsive in time.");
    }
}
