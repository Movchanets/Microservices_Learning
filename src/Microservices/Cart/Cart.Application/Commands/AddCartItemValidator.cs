using FluentValidation;

namespace Cart.Application.Commands;

public class AddCartItemValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.BuyerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
