using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Search.API.Models;
using Search.API.Services;

namespace Search.IntegrationTests;

public sealed class SearchDatabaseFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("docker.elastic.co/elasticsearch/elasticsearch:9.0.0")
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("xpack.security.enrollment.enabled", "false")
        .WithEnvironment("ES_JAVA_OPTS", "-Xms256m -Xmx256m")
        .WithPortBinding(9200, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request
                .ForPath("/_cluster/health")
                .ForPort(9200)
                .ForStatusCode(System.Net.HttpStatusCode.OK)))
        .Build();

    public ElasticsearchClient Client { get; private set; } = null!;
    public ISearchService SearchService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var port = _container.GetMappedPublicPort(9200);
        var uri = new Uri($"http://localhost:{port}");

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
