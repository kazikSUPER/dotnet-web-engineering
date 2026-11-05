using Bakery.Reviews.Application.Commands;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.Id).IsValidObjectId().WithName("Ідентифікатор відгуку");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0).WithMessage("Очікувана версія має бути додатною.");
    }
}
