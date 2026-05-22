using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.ActivateProduct;

public sealed record ActivateProductCommand(Guid ProductId) : IRequest<Result<Guid>>;
