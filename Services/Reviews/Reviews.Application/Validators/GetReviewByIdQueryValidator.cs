using Bakery.Reviews.Application.Queries;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator()
    {
        RuleFor(x => x.Id).IsValidObjectId().WithName("Ідентифікатор відгуку");
    }
}
