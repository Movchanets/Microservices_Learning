using Search.API.Models;

namespace Search.API.Services;

public interface ISearchService
{
    Task IndexProductAsync(ProductSearchDocument product, CancellationToken ct = default);
    Task UpdateProductMetadataAsync(UpdateProductMetadataRequest request, CancellationToken ct = default);
    Task UpdateProductPriceAsync(Guid productId, decimal price, string currency, CancellationToken ct = default);
    Task AddSkuToProductAsync(Guid productId, decimal price, string currency, CancellationToken ct = default);
    Task RemoveSkuFromProductAsync(Guid productId, CancellationToken ct = default);
    Task UpdateProductImageUrlAsync(Guid productId, string? imageUrl, CancellationToken ct = default);

    /// <summary>
    /// Adds a value to a variant axis on a product's search document.
    /// Creates the axis if it doesn't exist, appends the value if it does.
    /// </summary>
    Task AddVariantAxisValueAsync(
        Guid productId, string axisKey, string axisValue,
        CancellationToken ct = default);

    Task DeleteProductAsync(Guid productId, CancellationToken ct = default);
    Task<SearchResult<ProductSearchDocument>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
