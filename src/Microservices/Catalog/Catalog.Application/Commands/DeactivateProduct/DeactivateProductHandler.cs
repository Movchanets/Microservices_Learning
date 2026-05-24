using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Aggregates;
using MediatR;

namespace Catalog.Application.Commands.DeactivateProduct;

public sealed class DeactivateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        DeactivateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<Guid>.Failure("Product not found", "NOT_FOUND");

        product.Deactivate();

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}
