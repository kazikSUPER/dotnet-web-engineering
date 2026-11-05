using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.ValueObjects;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id).IsValidObjectId().WithName("Ідентифікатор відгуку");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0).WithMessage("Очікувана версія має бути додатною.");

        RuleFor(x => x.Comment)
            .NotEmpty()
            .Length(ReviewComment.MinLength, ReviewComment.MaxLength);

        RuleFor(x => x.Taste)
            .InclusiveBetween(Rating.MinScore, Rating.MaxScore).WithMessage("Оцінка смаку має бути від 1 до 5.");

        RuleFor(x => x.Freshness)
            .InclusiveBetween(Rating.MinScore, Rating.MaxScore).WithMessage("Оцінка свіжості має бути від 1 до 5.");

        RuleFor(x => x.Aroma)
            .InclusiveBetween(Rating.MinScore, Rating.MaxScore).WithMessage("Оцінка аромату має бути від 1 до 5.");

        RuleFor(x => x.Tags)
            .Must(tags => tags is null || tags.Count <= Review.MaxTags)
            .WithMessage($"Відгук може містити не більше {Review.MaxTags} тегів.");

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(Review.MaxTagLength);
    }
}
