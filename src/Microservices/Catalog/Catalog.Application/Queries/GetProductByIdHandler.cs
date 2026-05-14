using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

public sealed class GetProductByIdHandler(
    IProductReadRepository readRepository)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken) =>
        await readRepository.GetByIdAsync(request.ProductId, cancellationToken);
}
