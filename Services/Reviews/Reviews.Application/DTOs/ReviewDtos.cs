namespace Bakery.Reviews.Application.DTOs;

public sealed class RatingDto
{
    public int Taste { get; init; }

    public int Freshness { get; init; }

    public int Aroma { get; init; }

    public double Average { get; init; }
}

public sealed class AdminResponseDto
{
    public string ModeratorId { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTime RespondedAt { get; init; }
}

public sealed class ReviewDto
{
    public string Id { get; init; } = string.Empty;

    public int OrderId { get; init; }

    public int CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string Comment { get; init; } = string.Empty;

    public RatingDto Ratings { get; init; } = new();

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string Status { get; init; } = string.Empty;

    public AdminResponseDto? AdminResponse { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public int Version { get; init; }
}

public sealed class ReviewListItemDto
{
    public string Id { get; init; } = string.Empty;

    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public RatingDto Ratings { get; init; } = new();

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public int Version { get; init; }
}

public sealed class ReviewPageDto
{
    public IReadOnlyList<ReviewListItemDto> Items { get; init; } = [];

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }
}
