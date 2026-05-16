using FluentValidation;
using System.Text.RegularExpressions;

namespace Catalog.Application.Commands.CreateProduct;

public sealed partial class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(SkuRegex()).WithMessage("Invalid SKU format. Use alphanumeric and hyphens.");
    }

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9\-]{1,48}[A-Z0-9]$", RegexOptions.Compiled)]
    private static partial Regex SkuRegex();
}
