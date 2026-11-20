using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Exceptions;

namespace Bakery.Reviews.Domain.ValueObjects;

public sealed class ReviewComment : ValueObject
{
    public const int MinLength = 10;
    public const int MaxLength = 1000;

    private ReviewComment()
    {
        Text = null!;
    }

    public ReviewComment(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();

        if (trimmed.Length < MinLength)
        {
            throw new DomainException($"Коментар відгуку має містити щонайменше {MinLength} символів.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Коментар відгуку не може перевищувати {MaxLength} символів.");
        }

        Text = trimmed;
    }

    public string Text { get; }

    public bool Contains(string term, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        return Text.Contains(term, comparison);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Text;
    }

    public override string ToString()
    {
        return Text;
    }
}
