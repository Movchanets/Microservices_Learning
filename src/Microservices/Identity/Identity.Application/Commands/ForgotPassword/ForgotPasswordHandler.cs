using BuildingBlocks.Infrastructure.Models;
using Identity.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Handles the <see cref="ForgotPasswordCommand"/>.
/// </summary>
public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="logger">The logger.</param>
    public ForgotPasswordHandler(IUserRepository userRepository, ILogger<ForgotPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the forgot password request.
    /// Rationale: For security reasons, we always return success even if the user doesn't exist
    /// to prevent email enumeration attacks. In a real implementation, this would trigger
    /// an email with a reset token.
    /// </summary>
    /// <param name="request">The forgot password request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result regardless of whether the user was found.</returns>
    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing forgot password request for {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Forgot password requested for non-existent user: {Email}", request.Email);
            // Return success to avoid email enumeration
            return Result<bool>.Success(true);
        }

        // TODO: Generate password reset token and send email in Phase 5 (Notifications)
        _logger.LogInformation("Password reset would be initiated for {Email}", request.Email);

        return Result<bool>.Success(true);
    }
}
