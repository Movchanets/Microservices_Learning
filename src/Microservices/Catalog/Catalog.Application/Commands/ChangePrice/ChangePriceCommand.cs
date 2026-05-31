using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.ChangePrice;

public sealed record ChangePriceCommand(
    Guid ProductId,
    Guid SkuId,
    decimal NewPrice,
    string Currency) : IRequest<Result<bool>>;
