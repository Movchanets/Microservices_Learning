using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Commands.CreateCategory;

public sealed class CreateCategoryHandler(
    ICategoryRepository categoryRepository,
    BuildingBlocks.SharedContracts.Abstractions.IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = Category.Create(
            request.Name,
            request.Description,
            request.ParentCategoryId,
            request.SortOrder);

        categoryRepository.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.Slug,
            category.SortOrder,
            category.IsActive));
    }
}
