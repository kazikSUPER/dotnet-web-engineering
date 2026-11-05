using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Domain.ValueObjects;
using FluentValidation;

namespace Bakery.Reviews.Application.Validators;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository reviews)
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("Ідентифікатор замовлення має бути додатним.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Ідентифікатор клієнта має бути додатним.");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Ідентифікатор товару має бути додатним.");

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .Length(PersonName.MinLength, PersonName.MaxLength);

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .Length(1, 100);

        RuleFor(x => x.Comment)
            .NotEmpty()
            .Length(ReviewComment.MinLength, ReviewComment.MaxLength);

        RuleFor(x => x.Taste)
            .InclusiveBetween(Rating.MinScore, Rating.MaxScore).WithMessage("Оцінка смаку має бути від 1 до 5.");

        RuleFor(x => x.Freshness)
            .InclusiveBetween(Rating.MinScore, Rating.MaxScore).WithMessage("Оцінка свіжості має бути від 1 до 5.");

        RuleFor(x => x.Aroma)
            .InclusiveBetween(Rating.MinScore, Rating.MaxScore).WithMessage("Оцінка аромату має бути від 1 до 5.");

        RuleFor(x => x.Tags)
            .Must(tags => tags is null || tags.Count <= Review.MaxTags)
            .WithMessage($"Відгук може містити не більше {Review.MaxTags} тегів.");

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(Review.MaxTagLength)
            .Matches("^[\\p{L}\\p{N}\\s#'\\-]+$").WithMessage("Тег може містити лише літери, цифри, пробіли та апостроф.");

        RuleFor(x => x)
            .MustAsync(async (command, ct) => !await reviews.ExistsForOrderProductAsync(command.OrderId, command.ProductId, ct))
            .WithMessage("Клієнт уже залишив відгук на цей товар у цьому замовленні.")
            .WithName("Відгук");
    }
}
