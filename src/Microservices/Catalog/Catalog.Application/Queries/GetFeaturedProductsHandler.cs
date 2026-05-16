using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

public sealed class GetFeaturedProductsHandler(
    IProductReadRepository readRepository)
    : IRequestHandler<GetFeaturedProductsQuery, List<ProductListDto>>
{
    public async Task<List<ProductListDto>> Handle(
        GetFeaturedProductsQuery request,
        CancellationToken cancellationToken)
    {
        // Get newest products, optionally filtered by tag
        var result = await readRepository.ListAsync(
            page: 1,
            pageSize: 20,
            ct: cancellationToken);

        var products = result.Items.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Tag))
        {
            // For tag filtering, we need full product data
            // For now, return newest products (tag filtering can be added to read repository later)
        }

        return products.Take(8).ToList();
    }
}
