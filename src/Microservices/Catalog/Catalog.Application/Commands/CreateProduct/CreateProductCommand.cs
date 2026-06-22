using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    Guid CategoryId,
    Guid StoreId,
    string? Brand = null,
    List<string>? Tags = null,
    string? ImageUrl = null,
    List<Guid>? VariantAxisIds = null) : IRequest<Result<ProductDto>>;
