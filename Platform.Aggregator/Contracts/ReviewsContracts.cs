namespace Platform.Aggregator.Contracts;

public sealed record ProductRatingContract
{
    public string Id { get; init; } = string.Empty;

    public int ProductId { get; init; }

    public int TotalReviews { get; init; }

    public double AverageScore { get; init; }

    public DateTime LastUpdated { get; init; }
}

public sealed record ReviewPageContract
{
    public ReviewListItemContract[] Items { get; init; } = [];

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }
}

public sealed record ReviewListItemContract
{
    public string Id { get; init; } = string.Empty;

    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public RatingContract Ratings { get; init; } = new();

    public string[] Tags { get; init; } = [];

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}

public sealed record RatingContract
{
    public int Taste { get; init; }

    public int Freshness { get; init; }

    public int Aroma { get; init; }

    public double Average { get; init; }
}
