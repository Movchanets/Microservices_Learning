using Elastic.Clients.Elasticsearch;

namespace Search.API.Services;

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

        var createResponse = await client.Indices.CreateAsync(IndexName, cancellationToken);
        if (!createResponse.IsValidResponse)
            logger.LogError("Failed to create Elasticsearch index {Index}: {Error}", IndexName, createResponse.DebugInformation);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
