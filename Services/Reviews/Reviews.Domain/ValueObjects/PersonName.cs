using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Exceptions;

namespace Bakery.Reviews.Domain.ValueObjects;

public sealed class PersonName : ValueObject
{
    public const int MinLength = 2;
    public const int MaxLength = 100;

    private PersonName()
    {
        Value = null!;
    }

    public PersonName(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Length is < MinLength or > MaxLength)
        {
            throw new DomainException($"Ім'я клієнта має бути від {MinLength} до {MaxLength} символів.");
        }

        Value = trimmed;
    }

    public string Value { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
