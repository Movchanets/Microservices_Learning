using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record GetProductsByIdsQuery(List<Guid> Ids) : IRequest<List<ProductListDto>>;
