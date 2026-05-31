using FluentValidation;

namespace Cart.Application.Commands;

public class RemoveCartItemValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemValidator()
    {
        // BuyerId is optional — anonymous users have no BuyerId
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SkuId).NotEmpty();
    }
}
