using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result<bool>>;
