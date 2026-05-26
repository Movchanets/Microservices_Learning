using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Aggregates;
using MediatR;

namespace Catalog.Application.Commands.RemoveSku;

public sealed class RemoveSkuHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveSkuCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveSkuCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetWithSkusAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<bool>.Failure("Product not found", "NOT_FOUND");

        try
        {
            product.RemoveSku(request.SkuId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message, "NOT_FOUND");
        }

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
