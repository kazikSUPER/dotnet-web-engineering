using Bakery.Reviews.Application.Queries;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class SearchReviewsQueryValidator : AbstractValidator<SearchReviewsQuery>
{
    public SearchReviewsQueryValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MinimumLength(2).WithMessage("Рядок пошуку має містити щонайменше 2 символи.")
            .MaximumLength(100).WithMessage("Рядок пошуку не може перевищувати 100 символів.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50).WithMessage("Розмір видачі пошуку має бути від 1 до 50.");
    }
}
