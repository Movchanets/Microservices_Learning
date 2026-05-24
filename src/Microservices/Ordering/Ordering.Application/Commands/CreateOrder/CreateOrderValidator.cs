using FluentValidation;

namespace Ordering.Application.Commands.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.BuyerId)
            .NotEmpty().WithMessage("BuyerId is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required");
            item.RuleFor(x => x.ProductName).NotEmpty().WithMessage("ProductName is required");
            item.RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("UnitPrice must be positive");
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive");
        });
    }
}
