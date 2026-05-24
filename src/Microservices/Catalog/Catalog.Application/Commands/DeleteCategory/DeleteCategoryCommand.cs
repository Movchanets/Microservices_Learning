using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result<bool>>;
