using FluentValidation;

namespace Cart.Application.Commands;

public class RemoveCartItemValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemValidator()
    {
        RuleFor(x => x.BuyerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
