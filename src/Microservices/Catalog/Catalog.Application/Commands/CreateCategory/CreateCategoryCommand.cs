using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description = null,
    Guid? ParentCategoryId = null,
    int SortOrder = 0) : IRequest<Result<CategoryDto>>;
