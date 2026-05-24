using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest<Result<Guid>>;
