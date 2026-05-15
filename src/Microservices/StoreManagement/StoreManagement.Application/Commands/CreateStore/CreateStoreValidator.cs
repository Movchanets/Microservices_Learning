using FluentValidation;

namespace StoreManagement.Application.Commands.CreateStore;

public sealed class CreateStoreValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
    }
}
