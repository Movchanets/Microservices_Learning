using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    string? Brand = null,
    List<string>? Tags = null,
    string? ImageUrl = null,
    List<Guid>? VariantAxisIds = null) : IRequest<Result<ProductDto>>;
