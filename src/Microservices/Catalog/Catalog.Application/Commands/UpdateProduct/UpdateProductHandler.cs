using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Commands.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<ProductDto>.Failure("Product not found", "NOT_FOUND");

        if (product.CategoryId != request.CategoryId)
        {
            if (!await categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
                return Result<ProductDto>.Failure("Category not found", "NOT_FOUND");
        }

        product.Update(
            request.Name,
            request.Description,
            request.CategoryId,
            request.Brand,
            request.Tags,
            request.ImageUrl);

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        return Result<ProductDto>.Success(new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.CategoryId,
            category?.Name ?? "",
            product.Status.ToString(),
            product.ImageUrl,
            product.Brand,
            product.StoreId,
            product.Tags,
            product.Skus
                .Where(s => s.IsActive)
                .Select(s => new SkuDto(
                    s.Id,
                    s.SkuCode,
                    s.Price.Amount,
                    s.Price.Currency,
                    s.Status.ToString(),
                    s.TypedAttributes,
                    s.FlexibleAttributes,
                    s.CreatedAt))
                .ToList(),
            product.CreatedAt,
            product.UpdatedAt));
    }
}
