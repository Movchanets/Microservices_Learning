using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Search.API.Models;

namespace Search.API.Services;

public sealed class ElasticsearchService(
    ElasticsearchClient elasticClient,
    ILogger<ElasticsearchService> logger)
    : ISearchService
{
    private const string IndexName = "marketplace-products";

    public async Task IndexProductAsync(ProductSearchDocument product, CancellationToken ct = default)
    {
        var response = await elasticClient.IndexAsync(product, IndexName, ct);
        if (!response.IsValidResponse)
            logger.LogError("Failed to index product {ProductId}: {Error}", product.Id, response.DebugInformation);
    }

    public async Task UpdateProductAsync(ProductSearchDocument product, CancellationToken ct = default)
    {
        var response = await elasticClient.UpdateAsync<ProductSearchDocument, ProductSearchDocument>(
            IndexName, product.Id.ToString(), u => u.Doc(product), ct);

        if (!response.IsValidResponse)
            logger.LogError("Failed to update product {ProductId}: {Error}", product.Id, response.DebugInformation);
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken ct = default)
    {
        var response = await elasticClient.DeleteAsync<ProductSearchDocument>(productId.ToString(), d => d.Index(IndexName), ct);
        if (!response.IsValidResponse)
            logger.LogError("Failed to delete product {ProductId}: {Error}", productId, response.DebugInformation);
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
        var response = await elasticClient.SearchAsync<ProductSearchDocument>(s => s
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
                    .Field(f => f.CategoryName)
                    .Size(50)))
                .Add("price_ranges", a => a.Range(r => r
                    .Field(f => f.Price)
                    .Ranges(
                        new Elastic.Clients.Elasticsearch.Aggregations.AggregationRange { To = 25 },
                        new Elastic.Clients.Elasticsearch.Aggregations.AggregationRange { From = 25, To = 50 },
                        new Elastic.Clients.Elasticsearch.Aggregations.AggregationRange { From = 50, To = 100 },
                        new Elastic.Clients.Elasticsearch.Aggregations.AggregationRange { From = 100, To = 500 },
                        new Elastic.Clients.Elasticsearch.Aggregations.AggregationRange { From = 500 }
                    )))
            )
            .Sort(so => so
                .Score(sc => sc.Order(SortOrder.Desc))
                .Field(f => f.CreatedAt, fs => fs.Order(SortOrder.Desc))),
            ct);

        if (!response.IsValidResponse)
        {
            logger.LogError("Search failed: {Error}", response.DebugInformation);
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
