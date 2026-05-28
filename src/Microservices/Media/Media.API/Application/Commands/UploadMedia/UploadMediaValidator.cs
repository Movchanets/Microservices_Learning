using FluentValidation;

namespace Media.API.Application.Commands.UploadMedia;

public sealed class UploadMediaValidator : AbstractValidator<UploadMediaCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "video/mp4"
    };

    public UploadMediaValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(500).WithMessage("File name must not exceed 500 characters.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(x => AllowedContentTypes.Contains(x!))
            .WithMessage(x => $"Content type '{x.ContentType}' is not allowed.");

        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("Target ID is required.");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("Target type is required.")
            .MaximumLength(100);

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream is required.");

        RuleFor(x => x.FileStream!.Length)
            .GreaterThan(0).WithMessage("File must not be empty.");
    }
}
