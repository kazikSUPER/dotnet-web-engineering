namespace Bakery.Reviews.Application.Caching;

public static class CacheKeys
{
    public const string Prefix = "reviews:";

    public static string Review(string id) => $"{Prefix}review:{id}";

    public static string ProductRating(int productId) => $"{Prefix}product-rating:{productId}";

    public static string ProductReviews(int productId) => $"{Prefix}product:{productId}:reviews";

    public static string AggregateOrderFull(int orderId) => $"aggregate:order:{orderId}:full";
}
