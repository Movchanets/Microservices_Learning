using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Aggregates;
using MediatR;

namespace Catalog.Application.Commands.ActivateProduct;

public sealed class ActivateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        ActivateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<Guid>.Failure("Product not found", "NOT_FOUND");

        product.Activate();

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}
