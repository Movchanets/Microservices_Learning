using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Aggregates;
using Catalog.Domain.ValueObjects;
using MediatR;

namespace Catalog.Application.Commands.ChangePrice;

public sealed class ChangePriceHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangePriceCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ChangePriceCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetWithSkusAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<bool>.Failure("Product not found", "NOT_FOUND");

        product.ChangeSkuPrice(request.SkuId, Money.Create(request.NewPrice, request.Currency));

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
