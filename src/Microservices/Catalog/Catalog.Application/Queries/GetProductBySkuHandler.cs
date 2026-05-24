using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

public sealed class GetProductBySkuHandler(
    IProductReadRepository readRepository)
    : IRequestHandler<GetProductBySkuQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(
        GetProductBySkuQuery request,
        CancellationToken cancellationToken) =>
        await readRepository.GetBySkuAsync(request.Sku, cancellationToken);
}
