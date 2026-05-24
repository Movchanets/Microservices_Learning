using FluentValidation;

namespace Catalog.Application.Commands.CreateReview;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.PhotoUrls).Must(x => x is null || x.Count <= 5)
            .WithMessage("Maximum 5 photos allowed.");
    }
}
