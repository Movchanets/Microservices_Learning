using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto?>;
