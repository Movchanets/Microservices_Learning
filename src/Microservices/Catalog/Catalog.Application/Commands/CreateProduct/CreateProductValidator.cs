using FluentValidation;
using System.Text.RegularExpressions;

namespace Catalog.Application.Commands.CreateProduct;

public sealed partial class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code");

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(SkuRegex())
            .WithMessage("SKU must contain only alphanumeric characters and hyphens");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category is required");

        RuleFor(x => x.SellerId)
            .NotEmpty()
            .WithMessage("Seller is required");
    }

    [GeneratedRegex(@"^[A-Za-z0-9\-]+$", RegexOptions.Compiled)]
    private static partial Regex SkuRegex();
}
