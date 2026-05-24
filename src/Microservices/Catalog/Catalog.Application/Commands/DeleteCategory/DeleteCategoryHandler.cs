using BuildingBlocks.Infrastructure.Models;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Commands.DeleteCategory;

public sealed class DeleteCategoryHandler(
    ICategoryRepository categoryRepository,
    BuildingBlocks.SharedContracts.Abstractions.IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result<bool>.Failure("Category not found.", "NOT_FOUND");
        }

        category.Deactivate();
        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
