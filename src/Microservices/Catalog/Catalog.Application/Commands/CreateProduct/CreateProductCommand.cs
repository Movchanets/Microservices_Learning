using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    Guid SellerId,
    List<string>? Tags = null,
    string? ImageUrl = null) : IRequest<Result<ProductDto>>;
