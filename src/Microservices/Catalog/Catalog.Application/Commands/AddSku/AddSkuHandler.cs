using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Commands.AddSku;

public sealed class AddSkuHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<AddSkuCommand, Result<SkuDto>>
{
    public async Task<Result<SkuDto>> Handle(
        AddSkuCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load product with SKUs
        var product = await productRepository.GetWithSkusAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<SkuDto>.Failure("Product not found", "NOT_FOUND");

        // 2. Validate required attributes against category definitions
        var category = await categoryRepository.GetWithAttributeDefinitionsAsync(
            product.CategoryId, cancellationToken);

        if (category is not null)
        {
            try
            {
                category.ValidateRequiredAttributes(
                    Domain.Enums.AttributeTarget.Sku,
                    request.TypedAttributes ?? [],
                    request.FlexibleAttributes ?? []);
            }
            catch (InvalidOperationException ex)
            {
                return Result<SkuDto>.Failure(ex.Message, "VALIDATION_ERROR");
            }
        }

        // 3. Create SKU
        var price = Money.Create(request.Price, request.Currency);
        Sku sku;
        try
        {
            sku = product.AddSku(request.SkuCode, price, request.TypedAttributes ?? [], request.FlexibleAttributes);
        }
        catch (InvalidOperationException ex)
        {
            return Result<SkuDto>.Failure(ex.Message, "DUPLICATE_SKU");
        }

        // 4. Save — the product is already tracked, but EF Core doesn't auto-detect
        // new items added to a backing-field collection. Use context.Add() to explicitly
        // mark the Sku (and its owned Price) as Added.
        // Don't call productRepository.Update() — it marks the entire aggregate as Modified,
        // causing EF Core to generate UPDATE instead of INSERT for the new Sku.
        product.ClearDomainEvents();
        var context = (DbContext)unitOfWork;
        context.Add(sku);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Publish integration event directly (bypassing domain event + interceptor)
        await publishEndpoint.Publish(new SkuCreatedIntegrationEvent(
            ProductId: product.Id,
            SkuId: sku.Id,
            SkuCode: sku.SkuCode,
            ProductName: product.Name,
            StoreId: product.StoreId,
            Price: sku.Price.Amount,
            Currency: sku.Price.Currency,
            TypedAttributes: sku.TypedAttributes,
            FlexibleAttributes: sku.FlexibleAttributes,
            Timestamp: DateTime.UtcNow), cancellationToken);

        // 6. Return DTO
        return Result<SkuDto>.Success(new SkuDto(
            sku.Id,
            sku.SkuCode,
            sku.Price.Amount,
            sku.Price.Currency,
            sku.Status.ToString(),
            sku.ImageUrl,
            sku.TypedAttributes,
            sku.FlexibleAttributes,
            sku.CreatedAt));
    }
}
