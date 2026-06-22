using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Search.API.Models;
using Search.API.Services;
using Testcontainers.Elasticsearch;

namespace Search.IntegrationTests;

public sealed class SearchDatabaseFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:9.0.0")
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("xpack.security.enrollment.enabled", "false")
        .WithEnvironment("ES_JAVA_OPTS", "-Xms256m -Xmx256m")
        .Build();

    public ElasticsearchClient Client { get; private set; } = null!;
    public ISearchService SearchService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var uri = new Uri(_container.GetConnectionString());

        var settings = new ElasticsearchClientSettings(uri)
            .DefaultIndex("marketplace-products")
            .EnableDebugMode();

        Client = new ElasticsearchClient(settings);

        // Pre-create the index so ElasticsearchService constructor doesn't block
        var existsResponse = await Client.Indices.ExistsAsync("marketplace-products");
        if (!existsResponse.Exists)
        {
            await Client.Indices.CreateAsync("marketplace-products");
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ElasticsearchService>();

        SearchService = new ElasticsearchService(Client, logger);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Search collection")]
public class SearchCollection : ICollectionFixture<SearchDatabaseFixture>
{
}
