using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Commands.UpdateProfile;

public sealed class UpdateProfileHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProfileCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<Guid>.Failure("User not found.", "USER_NOT_FOUND");
        }

        // Check if the email is being changed and if the new email is already taken
        if (user.Email.Value != command.Email)
        {
            if (await userRepository.ExistsAsync(command.Email, cancellationToken))
            {
                return Result<Guid>.Failure("Email already in use.", "DUPLICATE_EMAIL");
            }
        }

        user.UpdateProfile(command.FirstName, command.LastName, command.Email);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
