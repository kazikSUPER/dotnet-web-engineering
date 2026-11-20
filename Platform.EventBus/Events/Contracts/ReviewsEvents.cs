namespace Platform.EventBus.Events.Contracts;

[EventRoutingKey("reviews.review.created")]
public sealed record ReviewCreatedEvent(
    string ReviewId,
    int ProductId,
    int CustomerId,
    int Rating,
    double NewAverageRating,
    int TotalReviews) : IntegrationEvent;

[EventRoutingKey("reviews.review.moderated")]
public sealed record ReviewModeratedEvent(
    string ReviewId,
    int ProductId,
    string Action,
    double NewAverageRating,
    int TotalReviews) : IntegrationEvent;
