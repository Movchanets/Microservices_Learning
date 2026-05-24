using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Search.API.Models;
using SearchRequest = Search.API.Models.SearchRequest;

namespace Search.API.Services;

public sealed class ElasticsearchService(
    ElasticsearchClient client,
    ILogger<ElasticsearchService> logger)
    : ISearchService
{
    private const string IndexName = "marketplace-products";

    public async Task IndexProductAsync(ProductSearchDocument product, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(product, i => i
            .Index(IndexName)
            .Id(product.Id.ToString()), ct);

        if (!response.IsValidResponse)
            logger.LogError("Failed to index product {ProductId}: {Error}", product.Id, response.DebugInformation);
    }

    public async Task UpdateProductAsync(ProductSearchDocument product, CancellationToken ct = default) =>
        await IndexProductAsync(product, ct);

    public async Task DeleteProductAsync(Guid productId, CancellationToken ct = default)
    {
        var response = await client.DeleteAsync<ProductSearchDocument>(
            productId.ToString(),
            d => d.Index(IndexName),
            ct);

        if (!response.IsValidResponse)
            logger.LogWarning("Failed to delete product {ProductId}: {Error}", productId, response.DebugInformation);
    }

    public async Task<SearchResult<ProductSearchDocument>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var (_, _, _, _, _, _, _, _, page, pageSize) = request;

        try
        {
            var response = await client.SearchAsync<ProductSearchDocument>(BuildSearchQuery(request), ct);

            if (!response.IsValidResponse)
            {
                logger.LogError("Search failed: {Error}", response.DebugInformation);
                return new SearchResult<ProductSearchDocument>([], 0, page, pageSize);
            }

            var facets = ExtractFacets(response);

            return new SearchResult<ProductSearchDocument>(
                response.Documents.ToList(),
                response.Total,
                page,
                pageSize,
                facets);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Elasticsearch search request was cancelled");
            return new SearchResult<ProductSearchDocument>([], 0, page, pageSize);
        }
    }

    private Action<SearchRequestDescriptor<ProductSearchDocument>> BuildSearchQuery(SearchRequest request)
    {
        var (query, categoryId, priceMin, priceMax, tags, brand, minRating, inStock, page, pageSize) = request;

        return s => s
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
                    mustQueries.Add(mq => mq.Term(t => t.Field("categoryId.keyword").Value(categoryId.Value.ToString())));
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

                // Brand filter
                if (!string.IsNullOrWhiteSpace(brand))
                {
                    mustQueries.Add(mq => mq.Term(t => t.Field("brand.keyword").Value(brand)));
                }

                // Rating filter
                if (minRating.HasValue)
                {
                    mustQueries.Add(mq => mq.Range(r => r
                        .NumberRange(nr => nr
                            .Field(f => f.Rating)
                            .Gte(minRating.Value))));
                }

                // In-stock filter
                if (inStock == true)
                {
                    mustQueries.Add(mq => mq.Term(t => t.Field(f => f.InStock).Value(true)));
                }

                b.Must(mustQueries.ToArray());
            }))
            .Aggregations(aggs => aggs
                .Add("categories", a => a.Terms(t => t
                    .Field("categoryName.keyword")
                    .Size(50)))
                .Add("brands", a => a.Terms(t => t
                    .Field("brand.keyword")
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
                    .UnmappedType(Elastic.Clients.Elasticsearch.Mapping.FieldType.Date)));
    }

    private static Dictionary<string, List<FacetValue>> ExtractFacets(SearchResponse<ProductSearchDocument> response)
    {
        var facets = new Dictionary<string, List<FacetValue>>();
        if (response.Aggregations == null)
            return facets;

        var categoryAgg = response.Aggregations.GetStringTerms("categories");
        if (categoryAgg != null)
        {
            facets["categories"] = categoryAgg.Buckets
                .Select(b => new FacetValue(b.Key.ToString(), b.DocCount))
                .ToList();
        }

        var brandAgg = response.Aggregations.GetStringTerms("brands");
        if (brandAgg != null)
        {
            facets["brands"] = brandAgg.Buckets
                .Where(b => !string.IsNullOrEmpty(b.Key.ToString()))
                .Select(b => new FacetValue(b.Key.ToString(), b.DocCount))
                .ToList();
        }

        var priceRangeAgg = response.Aggregations.GetRange("price_ranges");
        if (priceRangeAgg != null)
        {
            facets["price_ranges"] = priceRangeAgg.Buckets
                .Select(b => new FacetValue(b.Key, b.DocCount))
                .ToList();
        }

        return facets;
    }
}
