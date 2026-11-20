using FluentValidation;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Validators;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> IsValidObjectId<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Must(BeObjectId).WithMessage("{PropertyName} має коректний формат ObjectId.");
    }

    public static IRuleBuilderOptions<T, string?> IsValidOptionalObjectId<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(BeOptionalObjectId).WithMessage("{PropertyName} має коректний формат ObjectId.");
    }

    private static bool BeObjectId(string? value) => ObjectId.TryParse(value, out _);

    private static bool BeOptionalObjectId(string? value) => string.IsNullOrWhiteSpace(value) || ObjectId.TryParse(value, out _);
}
