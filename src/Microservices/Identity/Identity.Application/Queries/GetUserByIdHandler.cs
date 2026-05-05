using Identity.Application.DTOs;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Queries;

public sealed class GetUserByIdHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return null;

        return new UserDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAt);
    }
}
