using FluentValidation;

namespace Media.API.Application.Commands.DeleteMedia;

public sealed class DeleteMediaValidator : AbstractValidator<DeleteMediaCommand>
{
    public DeleteMediaValidator()
    {
        RuleFor(x => x.MediaItemId)
            .NotEmpty().WithMessage("Media item ID is required.");
    }
}
