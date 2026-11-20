using Bakery.Reviews.Domain.Enums;

namespace Bakery.Reviews.Domain.Filters;

public sealed record ReviewFilter
{
    public int? ProductId { get; init; }

    public int? CustomerId { get; init; }

    public ReviewStatus? Status { get; init; }

    public double? MinAverage { get; init; }

    public string? Tag { get; init; }

    public ReviewSortField Sort { get; init; } = ReviewSortField.CreatedAt;

    public bool Descending { get; init; } = true;
}
