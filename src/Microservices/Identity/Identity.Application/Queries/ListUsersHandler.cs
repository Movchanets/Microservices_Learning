using Identity.Application.DTOs;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Queries;

public sealed class ListUsersHandler(IUserRepository userRepository)
    : IRequestHandler<ListUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return users.Select(u => new UserDto(
            u.Id,
            u.Email.Value,
            u.FirstName,
            u.LastName,
            u.Role.ToString(),
            u.IsActive,
            u.CreatedAt)).ToList();
    }
}
