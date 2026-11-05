using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Filters;
using Bakery.Reviews.Domain.ValueObjects;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class GetReviewsQueryValidator : AbstractValidator<GetReviewsQuery>
{
    public GetReviewsQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Ідентифікатор товару має бути додатним.")
            .When(x => x.ProductId.HasValue);

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Ідентифікатор клієнта має бути додатним.")
            .When(x => x.CustomerId.HasValue);

        RuleFor(x => x.MinAverage)
            .InclusiveBetween((double)Rating.MinScore, Rating.MaxScore).WithMessage("Середня оцінка має бути від 1 до 5.")
            .When(x => x.MinAverage.HasValue);

        RuleFor(x => x.Tag)
            .NotEmpty()
            .MaximumLength(Review.MaxTagLength)
            .When(x => x.Tag is not null);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("Розмір сторінки має бути від 1 до 100.");

        RuleFor(x => x.Cursor)
            .Must(CursorToken.IsValid).WithMessage("Некоректний курсор пагінації.");
    }
}
