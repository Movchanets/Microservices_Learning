using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

public sealed class GetProductsByIdsHandler(
    IProductReadRepository readRepository)
    : IRequestHandler<GetProductsByIdsQuery, List<ProductListDto>>
{
    public async Task<List<ProductListDto>> Handle(
        GetProductsByIdsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Ids.Count == 0)
            return [];

        return await readRepository.GetByIdsAsync(request.Ids, cancellationToken);
    }
}
