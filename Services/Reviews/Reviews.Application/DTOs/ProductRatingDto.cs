namespace Bakery.Reviews.Application.DTOs;

public sealed class ProductRatingDto
{
    public string Id { get; init; } = string.Empty;

    public int ProductId { get; init; }

    public int TotalReviews { get; init; }

    public double AverageScore { get; init; }

    public DateTime LastUpdated { get; init; }

    public int Version { get; init; }
}
