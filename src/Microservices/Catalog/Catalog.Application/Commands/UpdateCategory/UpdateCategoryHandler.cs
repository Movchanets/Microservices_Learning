using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICategoryRepository categoryRepository,
    BuildingBlocks.SharedContracts.Abstractions.IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result<CategoryDto>.Failure("Category not found.", "NOT_FOUND");
        }

        category.Update(request.Name, request.Description, request.SortOrder);

        categoryRepository.Update(category);
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
