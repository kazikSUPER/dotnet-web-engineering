namespace Catalog.BLL.Caching;

public static class CacheKeys
{
    public const string Prefix = "catalog:";

    public static string Product(int id) => $"{Prefix}product:{id}";

    public static string ProductCard(int id) => $"{Prefix}product-card:{id}";

    public static string ProductsByCategory(int categoryId, DateTime? notEarlierThan) =>
        notEarlierThan is null
            ? $"{Prefix}products:category:{categoryId}"
            : $"{Prefix}products:category:{categoryId}:since:{notEarlierThan.Value:yyyyMMddHHmm}";

    public static string ProductsAll() => $"{Prefix}products:list";
}
