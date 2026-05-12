using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Command to initiate the forgot password process for a given email address.
/// </summary>
/// <param name="Email">The email address of the user who forgot their password.</param>
public record ForgotPasswordCommand(string Email) : IRequest<Result<bool>>;
