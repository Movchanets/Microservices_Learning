using FluentValidation;

namespace Media.API.Application.Commands.UpdateGalleryOrder;

public sealed class UpdateGalleryOrderValidator : AbstractValidator<UpdateGalleryOrderCommand>
{
    public UpdateGalleryOrderValidator()
    {
        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("Target ID is required.");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("Target type is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one order item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.MediaItemId).NotEmpty();
            item.RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        });
    }
}
