using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record GetProductBySkuQuery(string Sku) : IRequest<ProductDto?>;
