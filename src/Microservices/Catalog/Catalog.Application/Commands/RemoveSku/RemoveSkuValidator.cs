using FluentValidation;

namespace Catalog.Application.Commands.RemoveSku;

public sealed class RemoveSkuValidator : AbstractValidator<RemoveSkuCommand>
{
    public RemoveSkuValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SkuId).NotEmpty();
    }
}
