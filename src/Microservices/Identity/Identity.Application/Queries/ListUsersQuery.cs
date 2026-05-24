using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries;

public sealed record ListUsersQuery : IRequest<List<UserDto>>;
