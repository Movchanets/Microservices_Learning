using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Commands.AddSku;

public sealed record AddSkuCommand(
    Guid ProductId,
    string SkuCode,
    decimal Price,
    string Currency,
    Dictionary<string, string> TypedAttributes,
    Dictionary<string, string>? FlexibleAttributes = null) : IRequest<Result<SkuDto>>;
