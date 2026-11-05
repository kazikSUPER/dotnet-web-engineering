using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Domain.Enums;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class ModerateReviewCommandValidator : AbstractValidator<ModerateReviewCommand>
{
    public ModerateReviewCommandValidator()
    {
        RuleFor(x => x.Id).IsValidObjectId().WithName("Ідентифікатор відгуку");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Невідома дія модератора.");

        RuleFor(x => x.Action)
            .NotEqual(ModerationAction.Replied).WithMessage("Відповідь адміністратора додається окремою командою.");

        RuleFor(x => x.Moderator)
            .NotEmpty()
            .Length(1, 50).WithMessage("Ім'я модератора має бути від 1 до 50 символів.");

        RuleFor(x => x.Notes)
            .MaximumLength(250).WithMessage("Нотатка модератора не може перевищувати 250 символів.");
    }
}
