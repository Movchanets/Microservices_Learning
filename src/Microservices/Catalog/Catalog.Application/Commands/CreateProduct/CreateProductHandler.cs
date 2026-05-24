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
        // 1. Verify SKU uniqueness
        if (await productRepository.ExistsBySkuAsync(request.Sku, cancellationToken))
        {
            return Result<ProductDto>.Failure($"SKU '{request.Sku}' already exists.", "SKU_DUPLICATE");
        }

        // 2. Verify Category exists
        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<ProductDto>.Failure("Category not found.", "NOT_FOUND");
        }

        // 3. Create aggregate
        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.Currency,
            request.Sku,
            request.CategoryId,
            request.StoreId,
            request.Tags,
            request.ImageUrl);

        // 4. Save
        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Return DTO
        return Result<ProductDto>.Success(new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.Sku.Value,
            product.CategoryId,
            category.Name,
            product.Status.ToString(),
            product.ImageUrl,
            product.StoreId,
            product.Tags,
            product.CreatedAt,
            product.UpdatedAt));
    }
}
