using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Search.API.Models;

namespace Search.API.Services;

public sealed class ElasticsearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private const string IndexName = "marketplace-products";

    public ElasticsearchService(
        ElasticsearchClient client,
        ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;

        EnsureIndexAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureIndexAsync(CancellationToken ct = default)
    {
        var existsResponse = await _client.Indices.ExistsAsync(IndexName, ct);
        if (existsResponse.Exists)
            return;

        var createResponse = await _client.Indices.CreateAsync(IndexName, ct);
        if (!createResponse.IsValidResponse)
            _logger.LogError("Failed to create Elasticsearch index {Index}: {Error}", IndexName, createResponse.DebugInformation);
    }

    public async Task IndexProductAsync(ProductSearchDocument product, CancellationToken ct = default)
    {
        var response = await _client.IndexAsync(product, i => i
            .Index(IndexName)
            .Id(product.Id.ToString()), ct);

        if (!response.IsValidResponse)
            _logger.LogError("Failed to index product {ProductId}: {Error}", product.Id, response.DebugInformation);
    }

    public async Task UpdateProductAsync(ProductSearchDocument product, CancellationToken ct = default) =>
        await IndexProductAsync(product, ct);

    public async Task DeleteProductAsync(Guid productId, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync<ProductSearchDocument>(
            productId.ToString(),
            d => d.Index(IndexName),
            ct);

        if (!response.IsValidResponse)
            _logger.LogWarning("Failed to delete product {ProductId}: {Error}", productId, response.DebugInformation);
    }

    public async Task<SearchResult<ProductSearchDocument>> SearchAsync(
        string? query,
        Guid? categoryId,
        decimal? priceMin,
        decimal? priceMax,
        List<string>? tags,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var response = await _client.SearchAsync<ProductSearchDocument>(s => s
            .Indices(IndexName)
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q.Bool(b =>
            {
                var mustQueries = new List<Action<QueryDescriptor<ProductSearchDocument>>>();

                // Only active products
                mustQueries.Add(mq => mq.Term(t => t.Field(f => f.IsActive).Value(true)));

                // Full-text search
                if (!string.IsNullOrWhiteSpace(query))
                {
                    mustQueries.Add(mq => mq.MultiMatch(mm => mm
                        .Query(query)
                        .Fields(Elastic.Clients.Elasticsearch.Fields.FromStrings(new[] { "name^3", "description", "tags^2" }))
                        .Fuzziness(new Fuzziness("AUTO"))));
                }

                // Category filter
                if (categoryId.HasValue)
                {
                    mustQueries.Add(mq => mq.Term(t => t.Field(f => f.CategoryId).Value(categoryId.Value.ToString())));
                }

                // Price range filter
                if (priceMin.HasValue || priceMax.HasValue)
                {
                    mustQueries.Add(mq => mq.Range(r => r
                        .NumberRange(nr => nr
                            .Field(f => f.Price)
                            .Gte(priceMin.HasValue ? (double)priceMin.Value : null)
                            .Lte(priceMax.HasValue ? (double)priceMax.Value : null))));
                }

                // Tags filter
                if (tags is { Count: > 0 })
                {
                    mustQueries.Add(mq => mq.Terms(t => t
                        .Field(f => f.Tags)
                        .Terms(new TermsQueryField(tags.Select(FieldValue.String).ToArray()))));
                }

                b.Must(mustQueries.ToArray());
            }))
            // Aggregations for facets
            .Aggregations(aggs => aggs
                .Add("categories", a => a.Terms(t => t
                    .Field("categoryName.keyword")
                    .Size(50)))
                .Add("price_ranges", a => a.Range(r => r
                    .Field(f => f.Price)
                    .Ranges(
                        rr => rr.To(25),
                        rr => rr.From(25).To(50),
                        rr => rr.From(50).To(100),
                        rr => rr.From(100).To(500),
                        rr => rr.From(500)
                    )))
            )
            .Sort(so => so
                .Score(sc => sc.Order(SortOrder.Desc))
                .Field(f => f.CreatedAt, fs => fs
                    .Order(SortOrder.Desc)
                    .UnmappedType(Elastic.Clients.Elasticsearch.Mapping.FieldType.Date))),
            ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Search failed: {Error}", response.DebugInformation);
            return new SearchResult<ProductSearchDocument>([], 0, page, pageSize);
        }

        // Extract facets
        var facets = new Dictionary<string, List<FacetValue>>();
        if (response.Aggregations != null)
        {
            var categoryAgg = response.Aggregations.GetStringTerms("categories");
            if (categoryAgg != null)
            {
                facets["categories"] = categoryAgg.Buckets
                    .Select(b => new FacetValue(b.Key.ToString(), b.DocCount))
                    .ToList();
            }
        }

        return new SearchResult<ProductSearchDocument>(
            response.Documents.ToList(),
            response.Total,
            page,
            pageSize,
            facets);
    }
}
