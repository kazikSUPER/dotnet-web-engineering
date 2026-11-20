using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Exceptions;

namespace Bakery.Reviews.Domain.ValueObjects;

public sealed class Rating : ValueObject
{
    public const int MinScore = 1;
    public const int MaxScore = 5;

    private Rating()
    {
    }

    public Rating(int taste, int freshness, int aroma)
    {
        Taste = Validate(taste, "смак");
        Freshness = Validate(freshness, "свіжість");
        Aroma = Validate(aroma, "аромат");
    }

    public int Taste { get; }

    public int Freshness { get; }

    public int Aroma { get; }

    public double Average => Math.Round((Taste + Freshness + Aroma) / 3d, 2);

    public Rating With(int? taste = null, int? freshness = null, int? aroma = null)
    {
        return new Rating(taste ?? Taste, freshness ?? Freshness, aroma ?? Aroma);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Taste;
        yield return Freshness;
        yield return Aroma;
    }

    private static int Validate(int value, string label)
    {
        if (value is < MinScore or > MaxScore)
        {
            throw new DomainException($"Оцінка «{label}» має бути від {MinScore} до {MaxScore}.");
        }

        return value;
    }
}
