using FluentValidation;

namespace Media.API.Application.Commands.BulkLinkMedia;

public sealed class BulkLinkMediaValidator : AbstractValidator<BulkLinkMediaCommand>
{
    public BulkLinkMediaValidator()
    {
        RuleFor(x => x.MediaItemId)
            .NotEmpty()
            .WithMessage("MediaItemId is required.");

        RuleFor(x => x.SkuIds)
            .NotEmpty()
            .WithMessage("At least one SkuId must be provided.");
    }
}
