using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.RemoveSku;

public sealed record RemoveSkuCommand(
    Guid ProductId,
    Guid SkuId) : IRequest<Result<bool>>;
