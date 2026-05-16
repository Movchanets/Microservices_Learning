using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Commands.DeactivateUser;

public sealed class DeactivateUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateUserCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure("User not found.", "NOT_FOUND");
        }

        user.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
