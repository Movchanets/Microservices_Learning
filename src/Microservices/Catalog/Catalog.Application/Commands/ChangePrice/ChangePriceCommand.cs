using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.ChangePrice;

public sealed record ChangePriceCommand(
    Guid ProductId,
    decimal NewPrice,
    string Currency) : IRequest<Result<bool>>;
