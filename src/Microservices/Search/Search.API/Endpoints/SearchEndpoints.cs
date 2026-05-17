using Search.API.Models;
using Search.API.Services;

namespace Search.API.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search")
            .WithTags("Search")
            .WithOpenApi();

        group.MapGet("/products", async (
            ISearchService searchService,
            string? q,
            Guid? categoryId,
            decimal? priceMin,
            decimal? priceMax,
            string? tags,
            string? brand,
            double? minRating,
            bool? inStock,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var tagList = !string.IsNullOrWhiteSpace(tags)
                ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : null;

            var result = await searchService.SearchAsync(
                q,
                categoryId,
                priceMin,
                priceMax,
                tagList,
                brand,
                minRating,
                inStock,
                page > 0 ? page : 1,
                pageSize > 0 ? Math.Min(pageSize, 100) : 20,
                ct);

            return Results.Ok(result);
        })
        .WithName("SearchProducts")
        .Produces<SearchResult<ProductSearchDocument>>();
    }
}
