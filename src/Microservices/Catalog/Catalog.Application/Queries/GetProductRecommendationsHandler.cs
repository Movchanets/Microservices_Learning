using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

public sealed class GetProductRecommendationsHandler(
    IProductReadRepository readRepository)
    : IRequestHandler<GetProductRecommendationsQuery, List<ProductListDto>>
{
    public async Task<List<ProductListDto>> Handle(
        GetProductRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        // First, get the current product to find its category
        var product = await readRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return [];

        // Get products from the same category, excluding the current one
        var related = await readRepository.ListAsync(
            page: 1,
            pageSize: 4,
            categoryId: product.CategoryId,
            ct: cancellationToken);

        return related.Items
            .Where(p => p.Id != request.ProductId)
            .Take(3)
            .ToList();
    }
}
