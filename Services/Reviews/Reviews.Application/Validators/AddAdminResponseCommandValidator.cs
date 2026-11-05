using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Domain.ValueObjects;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class AddAdminResponseCommandValidator : AbstractValidator<AddAdminResponseCommand>
{
    public AddAdminResponseCommandValidator()
    {
        RuleFor(x => x.Id).IsValidObjectId().WithName("Ідентифікатор відгуку");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0).WithMessage("Очікувана версія має бути додатною.");

        RuleFor(x => x.ModeratorId)
            .NotEmpty()
            .Length(1, 50).WithMessage("Ідентифікатор модератора має бути від 1 до 50 символів.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .Length(AdminResponse.MinMessageLength, AdminResponse.MaxMessageLength);
    }
}
