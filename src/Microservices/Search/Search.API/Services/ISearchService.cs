using Search.API.Models;

namespace Search.API.Services;

public interface ISearchService
{
    Task IndexProductAsync(ProductSearchDocument product, CancellationToken ct = default);
    Task UpdateProductAsync(ProductSearchDocument product, CancellationToken ct = default);
    Task DeleteProductAsync(Guid productId, CancellationToken ct = default);
    Task<SearchResult<ProductSearchDocument>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
