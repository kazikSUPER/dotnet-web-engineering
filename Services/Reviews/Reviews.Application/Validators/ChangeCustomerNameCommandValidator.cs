using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Domain.ValueObjects;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class ChangeCustomerNameCommandValidator : AbstractValidator<ChangeCustomerNameCommand>
{
    public ChangeCustomerNameCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Ідентифікатор клієнта має бути додатним.");

        RuleFor(x => x.NewCustomerName)
            .NotEmpty()
            .Length(PersonName.MinLength, PersonName.MaxLength);
    }
}
