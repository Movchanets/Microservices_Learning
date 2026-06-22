using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

/// <summary>
/// Handles GetSkuByIdQuery: retrieves a single SKU with its product context
/// via the read-only repository. Returns null if not found.
/// </summary>
public sealed class GetSkuByIdHandler(IProductReadRepository readRepository)
    : IRequestHandler<GetSkuByIdQuery, SkuDto?>
{
    public async Task<SkuDto?> Handle(GetSkuByIdQuery request, CancellationToken cancellationToken)
    {
        return await readRepository.GetSkuByIdAsync(request.SkuId, cancellationToken);
    }
}
