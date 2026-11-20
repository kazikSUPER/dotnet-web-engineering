using Bakery.Reviews.Application.Commands;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class RenameProductCommandValidator : AbstractValidator<RenameProductCommand>
{
    public RenameProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Ідентифікатор товару має бути додатним.");

        RuleFor(x => x.NewProductName)
            .NotEmpty()
            .Length(1, 100).WithMessage("Назва товару має бути від 1 до 100 символів.");
    }
}
