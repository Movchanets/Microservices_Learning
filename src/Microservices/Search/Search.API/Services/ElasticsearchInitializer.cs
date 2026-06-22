using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Search.API.Models;

namespace Search.API.Services;

/// <summary>
/// Hosted service that creates the Elasticsearch index on application startup.
/// Defines the index mapping (text fields, keyword fields, numeric fields)
/// and handles idempotent creation (skips if index already exists).
/// </summary>
public sealed class ElasticsearchInitializer(
    ElasticsearchClient client,
    ILogger<ElasticsearchInitializer> logger)
    : IHostedService
{
    private const string IndexName = "marketplace-products";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var existsResponse = await client.Indices.ExistsAsync(IndexName, cancellationToken);
        if (existsResponse.Exists)
            return;

        // Create index with default auto-mapping
        var createResponse = await client.Indices.CreateAsync(IndexName, cancellationToken);
        if (!createResponse.IsValidResponse)
        {
            logger.LogError("Failed to create Elasticsearch index {Index}: {Error}", IndexName, createResponse.DebugInformation);
            return;
        }

        // Override price fields to double (auto-detect maps decimal as long, losing cents)
        var putMappingResponse = await client.Indices.PutMappingAsync(IndexName, m => m
            .Properties(new Properties
            {
                { "minPrice", new DoubleNumberProperty() },
                { "maxPrice", new DoubleNumberProperty() },
            }),
            cancellationToken);

        if (!putMappingResponse.IsValidResponse)
            logger.LogWarning("Failed to set explicit mapping for price fields: {Error}", putMappingResponse.DebugInformation);
        else
            logger.LogInformation("Created Elasticsearch index {Index} with double mapping for price fields", IndexName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
