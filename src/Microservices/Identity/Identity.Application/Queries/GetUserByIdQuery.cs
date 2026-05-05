using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;
