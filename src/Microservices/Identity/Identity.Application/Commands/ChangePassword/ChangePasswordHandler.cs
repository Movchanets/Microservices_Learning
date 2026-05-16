using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Commands.ChangePassword;

public sealed class ChangePasswordHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<Guid>.Failure("User not found.", "USER_NOT_FOUND");
        }

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash.Hash))
        {
            return Result<Guid>.Failure("Invalid current password.", "INVALID_PASSWORD");
        }

        var newPasswordHashed = passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newPasswordHashed);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
