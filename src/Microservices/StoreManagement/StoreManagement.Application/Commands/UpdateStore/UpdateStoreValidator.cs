using FluentValidation;

namespace StoreManagement.Application.Commands.UpdateStore;

public sealed class UpdateStoreValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
    }
}
