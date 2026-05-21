using FluentValidation;

namespace Cart.Application.Commands;

public class AddCartItemValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemValidator()
    {
        // BuyerId is optional — anonymous users have no BuyerId
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
