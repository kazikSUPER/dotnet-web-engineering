using Bakery.Reviews.Application.Queries;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class GetProductRatingQueryValidator : AbstractValidator<GetProductRatingQuery>
{
    public GetProductRatingQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Ідентифікатор товару має бути додатним.");
    }
}
