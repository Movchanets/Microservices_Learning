using FluentValidation;

namespace StoreManagement.Application.Commands.SetStoreLogo;

public sealed class SetStoreLogoValidator : AbstractValidator<SetStoreLogoCommand>
{
    public SetStoreLogoValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.LogoUrl).NotEmpty().MaximumLength(500);
    }
}
