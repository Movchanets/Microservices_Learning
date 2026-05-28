using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record GetSkuByIdQuery(Guid SkuId) : IRequest<SkuDto?>;
