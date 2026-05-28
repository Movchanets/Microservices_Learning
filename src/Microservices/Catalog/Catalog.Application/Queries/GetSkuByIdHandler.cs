using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

public sealed class GetSkuByIdHandler(IProductReadRepository readRepository)
    : IRequestHandler<GetSkuByIdQuery, SkuDto?>
{
    public async Task<SkuDto?> Handle(GetSkuByIdQuery request, CancellationToken cancellationToken)
    {
        return await readRepository.GetSkuByIdAsync(request.SkuId, cancellationToken);
    }
}
