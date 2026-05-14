using FluentValidation;

namespace Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Validator for the <see cref="ForgotPasswordCommand"/>.
/// </summary>
public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordValidator"/> class.
    /// </summary>
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
