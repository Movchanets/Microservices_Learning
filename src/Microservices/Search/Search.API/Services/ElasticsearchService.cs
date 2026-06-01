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

    // ── Index / Full Replace ──────────────────────────────────────

    public async Task IndexProductAsync(ProductSearchDocument product, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(product, i => i
            .Index(IndexName)
            .Id(product.Id.ToString()), ct);

        if (!response.IsValidResponse)
            logger.LogError("Failed to index product {ProductId}: {Error}", product.Id, response.DebugInformation);
    }

    // ── Partial Updates (all use RetryOnConflict) ─────────────────

    public async Task UpdateProductMetadataAsync(UpdateProductMetadataRequest request, CancellationToken ct = default)
    {
        var response = await client.UpdateAsync<ProductSearchDocument, object>(
            IndexName,
            request.ProductId.ToString(),
            u => u
                .RetryOnConflict(5)
                .Doc(new
                {
                    request.Name,
                    request.Description,
                    request.CategoryId,
                    request.CategoryName,
                    request.Tags,
                    request.ImageUrl,
                    request.StoreId,
                    request.IsActive,
                    request.UpdatedAt,
                    request.Brand,
                    Attributes = request.Attributes ?? new Dictionary<string, string>(),
                }),
            ct);

        if (!response.IsValidResponse)
            logger.LogWarning("Failed to update metadata for product {ProductId}: {Error}",
                request.ProductId, response.DebugInformation);
    }

    public async Task UpdateProductPriceAsync(Guid productId, decimal price, string currency, CancellationToken ct = default)
    {
        await UpdateWithScript(productId, ScriptConstants.ExtendPriceRange,
            ScriptConstants.PriceParams(price, currency), ct);
    }

    public async Task AddSkuToProductAsync(Guid productId, decimal price, string currency, CancellationToken ct = default)
    {
        await UpdateWithScript(productId, ScriptConstants.AddSku,
            ScriptConstants.PriceParams(price, currency), ct);
    }

    public async Task RemoveSkuFromProductAsync(Guid productId, CancellationToken ct = default)
    {
        await UpdateWithScript(productId, ScriptConstants.RemoveSku, null, ct);
    }

    public async Task UpdateProductImageUrlAsync(Guid productId, string? imageUrl, CancellationToken ct = default)
    {
        var response = await client.UpdateAsync<ProductSearchDocument, object>(
            IndexName,
            productId.ToString(),
            u => u
                .RetryOnConflict(5)
                .Doc(new { ImageUrl = imageUrl, UpdatedAt = DateTime.UtcNow }),
            ct);

        if (!response.IsValidResponse)
            logger.LogWarning("Failed to update ImageUrl for product {ProductId}: {Error}",
                productId, response.DebugInformation);
    }

    public async Task AddVariantAxisValueAsync(
        Guid productId, string axisKey, string axisValue,
        CancellationToken ct = default)
    {
        // Use script parameters to prevent injection. Values are passed as params, not interpolated.
        var script = @"
            if (ctx._source.variantAxes == null) {
                ctx._source.variantAxes = [:];
            }
            if (!ctx._source.variantAxes.containsKey(params.axisKey)) {
                ctx._source.variantAxes[params.axisKey] = [];
            }
            if (!ctx._source.variantAxes[params.axisKey].contains(params.axisValue)) {
                ctx._source.variantAxes[params.axisKey].add(params.axisValue);
            }";

        var parameters = new Dictionary<string, object>
        {
            ["axisKey"] = axisKey,
            ["axisValue"] = axisValue
        };

        var response = await client.UpdateAsync<ProductSearchDocument, object>(
            IndexName,
            productId.ToString(),
            u => u
                .RetryOnConflict(5)
                .Script(s => s.Source(script).Params(parameters)),
            ct);

        if (!response.IsValidResponse)
            logger.LogWarning(
                "Failed to add variant axis value for product {ProductId} ({Key}={Value}): {Error}",
                productId, axisKey, axisValue, response.DebugInformation);
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken ct = default)
    {
        var response = await client.DeleteAsync<ProductSearchDocument>(
            productId.ToString(),
            d => d.Index(IndexName),
            ct);

        if (!response.IsValidResponse)
            logger.LogWarning("Failed to delete product {ProductId}: {Error}", productId, response.DebugInformation);
    }

    // ── Search ────────────────────────────────────────────────────

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

            return new SearchResult<ProductSearchDocument>(
                response.Documents.ToList(),
                response.Total,
                page,
                pageSize,
                ExtractFacets(response));
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Elasticsearch search request was cancelled");
            return new SearchResult<ProductSearchDocument>([], 0, page, pageSize);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private async Task UpdateWithScript(Guid productId, string script, Dictionary<string, object>? parameters, CancellationToken ct)
    {
        var response = await client.UpdateAsync<ProductSearchDocument, object>(
            IndexName,
            productId.ToString(),
            u => u
                .RetryOnConflict(5)
                .Script(s =>
                {
                    s.Source(script);
                    if (parameters != null)
                        s.Params(parameters);
                }),
            ct);

        if (!response.IsValidResponse)
            logger.LogWarning("Failed to update product {ProductId} with script: {Error}",
                productId, response.DebugInformation);
    }

    // ── Query Building ────────────────────────────────────────────

    private Action<SearchRequestDescriptor<ProductSearchDocument>> BuildSearchQuery(SearchRequest request)
    {
        var (query, categoryId, priceMin, priceMax, tags, brand, minRating, inStock, page, pageSize) = request;

        return s => s
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q.Bool(b => b.Must(BuildFilters(query, categoryId, priceMin, priceMax, tags, brand, minRating, inStock).ToArray())))
            .Aggregations(aggs => aggs
                .Add("categories", a => a.Terms(t => t.Field("categoryName.keyword").Size(50)))
                .Add("brands", a => a.Terms(t => t.Field("brand.keyword").Size(50)))
                .Add("price_ranges", a => a.Range(r => r
                    .Field(f => f.MinPrice)
                    .Ranges(
                        rr => rr.To(25),
                        rr => rr.From(25).To(50),
                        rr => rr.From(50).To(100),
                        rr => rr.From(100).To(500),
                        rr => rr.From(500)))))
            .Sort(so => so
                .Score(sc => sc.Order(SortOrder.Desc))
                .Field(f => f.CreatedAt, fs => fs
                    .Order(SortOrder.Desc)
                    .UnmappedType(Elastic.Clients.Elasticsearch.Mapping.FieldType.Date)));
    }

    private static List<Action<QueryDescriptor<ProductSearchDocument>>> BuildFilters(
        string? query, Guid? categoryId, decimal? priceMin, decimal? priceMax,
        List<string>? tags, string? brand, double? minRating, bool? inStock)
    {
        var filters = new List<Action<QueryDescriptor<ProductSearchDocument>>>();

        filters.Add(f => f.Term(t => t.Field(f => f.IsActive).Value(true)));

        if (!string.IsNullOrWhiteSpace(query))
            filters.Add(f => f.MultiMatch(mm => mm
                .Query(query)
                .Fields(Elastic.Clients.Elasticsearch.Fields.FromStrings(["name^3", "description", "tags^2"]))
                .Fuzziness(new Fuzziness("AUTO"))));

        if (categoryId.HasValue)
            filters.Add(f => f.Term(t => t.Field("categoryId.keyword").Value(categoryId.Value.ToString())));

        // Price range overlap: product.MaxPrice >= priceMin AND product.MinPrice <= priceMax
        if (priceMin.HasValue)
            filters.Add(f => f.Range(r => r.Number(nr => nr.Field(f => f.MaxPrice).Gte((double)priceMin.Value))));

        if (priceMax.HasValue)
            filters.Add(f => f.Range(r => r.Number(nr => nr.Field(f => f.MinPrice).Lte((double)priceMax.Value))));

        if (tags is { Count: > 0 })
            filters.Add(f => f.Terms(t => t.Field(f => f.Tags).Terms(new TermsQueryField(tags.Select(FieldValue.String).ToArray()))));

        if (!string.IsNullOrWhiteSpace(brand))
            filters.Add(f => f.Term(t => t.Field("brand.keyword").Value(brand)));

        if (minRating.HasValue)
            filters.Add(f => f.Range(r => r.Number(nr => nr.Field(f => f.Rating).Gte(minRating.Value))));

        if (inStock == true)
            filters.Add(f => f.Term(t => t.Field(f => f.InStock).Value(true)));

        return filters;
    }

    // ── Facet Extraction ──────────────────────────────────────────

    private static Dictionary<string, List<FacetValue>> ExtractFacets(SearchResponse<ProductSearchDocument> response)
    {
        var facets = new Dictionary<string, List<FacetValue>>();
        if (response.Aggregations == null)
            return facets;

        AddStringTermsFacet(facets, response, "categories");
        AddStringTermsFacet(facets, response, "brands");
        AddRangeFacet(facets, response, "price_ranges");

        return facets;
    }

    private static void AddStringTermsFacet(
        Dictionary<string, List<FacetValue>> facets,
        SearchResponse<ProductSearchDocument> response,
        string key)
    {
        var agg = response.Aggregations!.GetStringTerms(key);
        if (agg?.Buckets == null) return;

        facets[key] = agg.Buckets
            .Where(b => !string.IsNullOrEmpty(b.Key.ToString()))
            .Select(b => new FacetValue(b.Key.ToString()!, b.DocCount))
            .ToList();
    }

    private static void AddRangeFacet(
        Dictionary<string, List<FacetValue>> facets,
        SearchResponse<ProductSearchDocument> response,
        string key)
    {
        var agg = response.Aggregations!.GetRange(key);
        if (agg?.Buckets == null) return;

        facets[key] = agg.Buckets
            .Select(b => new FacetValue(b.Key, b.DocCount))
            .ToList();
    }
}
