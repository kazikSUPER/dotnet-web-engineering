namespace Platform.EventBus.Topology;

public static class EventBusTopology
{
    public const string ExchangeName = "platform.events";
    public const string DeadLetterExchangeName = "platform.events.dlx";

    public const string OrdersCatalogEventsQueue = "q.orders.catalog-events";
    public const string BakeryCatalogEventsQueue = "q.bakery.catalog-events";
    public const string ReviewsCatalogEventsQueue = "q.reviews.catalog-events";
    public const string CatalogReviewEventsQueue = "q.catalog.review-events";

    public static string GetDeadLetterQueueName(string queueName) => $"{queueName}.dlq";
}
