using FluentValidation;

namespace Catalog.Application.Commands.AddSku;

public sealed class AddSkuValidator : AbstractValidator<AddSkuCommand>
{
    public AddSkuValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SkuCode)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[A-Za-z0-9][A-Za-z0-9\-]{1,48}[A-Za-z0-9]$")
            .WithMessage("Invalid SKU format. Use alphanumeric and hyphens only.");
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
