using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.Queries;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.UpdateUserRole;

public sealed class UpdateUserRoleHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserRoleCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        UpdateUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.", "NOT_FOUND");
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var newRole))
        {
            return Result<UserDto>.Failure($"Invalid role: {request.Role}", "INVALID_ROLE");
        }

        user.ChangeRole(newRole);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAt));
    }
}
