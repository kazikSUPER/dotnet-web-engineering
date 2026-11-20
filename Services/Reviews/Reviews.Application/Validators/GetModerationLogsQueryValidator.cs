using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Filters;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class GetModerationLogsQueryValidator : AbstractValidator<GetModerationLogsQuery>
{
    public GetModerationLogsQueryValidator()
    {
        RuleFor(x => x.ReviewId).IsValidOptionalObjectId().WithName("Ідентифікатор відгуку");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("Розмір сторінки має бути від 1 до 100.");

        RuleFor(x => x.Cursor)
            .Must(CursorToken.IsValid).WithMessage("Некоректний курсор пагінації.");
    }
}
