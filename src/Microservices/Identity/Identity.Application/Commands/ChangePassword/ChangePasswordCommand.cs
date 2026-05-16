using BuildingBlocks.Infrastructure.Models;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result<Guid>>;
