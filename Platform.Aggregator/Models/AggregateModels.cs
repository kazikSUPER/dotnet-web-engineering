using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Models;

public sealed record OrderAggregateDto(
    OrderContract Order,
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<ProductFeedbackDto> Feedback,
    IReadOnlyList<string> Warnings,
    int DownstreamCalls,
    double AssemblyDurationMilliseconds,
    DateTimeOffset AssembledAt)
{
    public CustomerContract? Customer { get; init; }

    public PaymentContract? Payment { get; init; }

    public OrderStatusHistoryContract[] StatusHistory { get; init; } = [];

    public IReadOnlyList<ReviewListItemContract> CustomerReviews { get; init; } = [];
}

public sealed record OrderLineDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    ProductCardContract? Catalog);

public sealed record ProductFeedbackDto(
    int ProductId,
    ProductRatingContract? Rating,
    IReadOnlyList<ReviewListItemContract> Reviews);

public sealed record ProductOverviewDto(
    int ProductId,
    ProductCardContract? Product,
    ProductRatingContract? Rating,
    IReadOnlyList<ReviewListItemContract> Reviews,
    IReadOnlyList<string> Warnings,
    int DownstreamCalls,
    double AssemblyDurationMilliseconds,
    DateTimeOffset AssembledAt);
