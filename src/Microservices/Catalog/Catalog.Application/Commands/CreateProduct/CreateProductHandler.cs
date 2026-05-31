using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify Category exists
        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<ProductDto>.Failure("Category not found.", "NOT_FOUND");
        }

        // 2. Create aggregate (SKUs are added separately via AddSku)
        var product = Product.Create(
            request.Name,
            request.Description,
            request.CategoryId,
            request.StoreId,
            request.Brand,
            request.Tags,
            request.ImageUrl);

        // 3. Save
        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Return DTO (product has no SKUs yet)
        return Result<ProductDto>.Success(new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.CategoryId,
            category.Name,
            product.Status.ToString(),
            product.ImageUrl,
            product.Brand,
            product.StoreId,
            product.Tags,
            [],  // No SKUs yet
            product.CreatedAt,
            product.UpdatedAt));
    }
}
