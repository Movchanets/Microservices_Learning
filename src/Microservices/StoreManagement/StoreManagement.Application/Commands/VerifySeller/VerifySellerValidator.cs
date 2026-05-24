using FluentValidation;

namespace StoreManagement.Application.Commands.VerifySeller;

public sealed class VerifySellerValidator : AbstractValidator<VerifySellerCommand>
{
    public VerifySellerValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000)
            .When(x => !x.IsApproved);
    }
}
