using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MassTransit;
using MediatR;

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

        List<string> variantAxisKeys = [];

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

            // Validate Select-type attribute values against AllowedValues
            var selectDefs = category.AttributeDefinitions
                .Where(a => a.Target == Domain.Enums.AttributeTarget.Sku
                    && a.ValueType == Domain.Enums.AttributeType.Select
                    && a.AllowedValues.Count > 0)
                .ToList();

            foreach (var def in selectDefs)
            {
                if (request.TypedAttributes?.TryGetValue(def.Key, out var value) == true
                    && !string.IsNullOrWhiteSpace(value))
                {
                    if (!def.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        return Result<SkuDto>.Failure(
                            $"Attribute '{def.DisplayName}' value '{value}' is not allowed. " +
                            $"Allowed values: {string.Join(", ", def.AllowedValues)}",
                            "VALIDATION_ERROR");
                    }
                }
            }

            // Collect variant-axis keys for uniqueness validation in AddSku
            variantAxisKeys = category.AttributeDefinitions
                .Where(a => a.Target == Domain.Enums.AttributeTarget.Sku && a.IsVariantAxis)
                .Select(a => a.Key)
                .ToList();
        }

        // 3. Create SKU (with variant uniqueness check)
        var price = Money.Create(request.Price, request.Currency);
        Sku sku;
        try
        {
            sku = product.AddSku(
                request.SkuCode, price,
                request.TypedAttributes ?? [], request.FlexibleAttributes,
                variantAxisKeys.Count > 0 ? variantAxisKeys : null);
        }
        catch (InvalidOperationException ex)
        {
            return Result<SkuDto>.Failure(ex.Message, "DUPLICATE_VARIANT");
        }

        // 4. Save — Guid v7 generates IDs on insert, EF Core detects new Skus
        // as Added via the Product.Skus backing field.
        product.ClearDomainEvents();
        productRepository.Update(product);
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
