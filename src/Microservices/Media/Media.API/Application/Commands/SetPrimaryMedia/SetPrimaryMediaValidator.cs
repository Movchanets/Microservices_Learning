using FluentValidation;

namespace Media.API.Application.Commands.SetPrimaryMedia;

public sealed class SetPrimaryMediaValidator : AbstractValidator<SetPrimaryMediaCommand>
{
    public SetPrimaryMediaValidator()
    {
        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("Target ID is required.");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("Target type is required.");

        RuleFor(x => x.MediaItemId)
            .NotEmpty().WithMessage("Media item ID is required.");
    }
}
