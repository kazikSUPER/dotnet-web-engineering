namespace Orders.BLL.Caching;

public static class CacheKeys
{
    public const string Prefix = "orders:";

    public static string Order(int id) => $"{Prefix}order:{id}";

    public static string OrderHistory(int id) => $"{Prefix}order:{id}:history";

    public static string OrdersByCustomer(int customerId, int page, int pageSize) =>
        $"{Prefix}customer:{customerId}:orders:p{page}:s{pageSize}";

    public static string Customer(int id) => $"{Prefix}customer:{id}";

    public static string CustomersList() => $"{Prefix}customers:list";

    public static string PaymentByOrder(int orderId) => $"{Prefix}order:{orderId}:payment";

    public static string AggregateOrderFull(int orderId) => $"aggregate:order:{orderId}:full";
}
