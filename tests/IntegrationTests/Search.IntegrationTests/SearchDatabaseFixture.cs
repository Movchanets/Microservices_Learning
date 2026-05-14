using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;
using Xunit;

namespace Search.IntegrationTests;

public class SearchDatabaseFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _elasticsearchContainer =
        new ElasticsearchBuilder()
            .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.11.3")
            .WithEnvironment("xpack.security.enabled", "false")
            .Build();

    public ElasticsearchClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _elasticsearchContainer.StartAsync();

        var settings = new ElasticsearchClientSettings(new Uri(_elasticsearchContainer.GetConnectionString()))
            .DefaultIndex("marketplace-products");

        Client = new ElasticsearchClient(settings);
    }

    public Task DisposeAsync()
    {
        return _elasticsearchContainer.DisposeAsync().AsTask();
    }
}
