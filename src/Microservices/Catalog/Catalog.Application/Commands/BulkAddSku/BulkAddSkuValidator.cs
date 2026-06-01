using FluentValidation;

namespace Catalog.Application.Commands.BulkAddSku;

public sealed class BulkAddSkuValidator : AbstractValidator<BulkAddSkuCommand>
{
    public BulkAddSkuValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");

        RuleFor(x => x.VariantCombinations)
            .NotEmpty()
            .WithMessage("At least one variant axis is required");

        RuleFor(x => x.VariantCombinations)
            .Must(combos => combos.All(c => c.Value.Count > 0))
            .WithMessage("Each variant axis must have at least one value")
            .When(x => x.VariantCombinations.Count > 0);

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BasePrice.HasValue)
            .WithMessage("Base price must be non-negative");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code");
    }
}
