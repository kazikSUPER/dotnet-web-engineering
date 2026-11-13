using System.Diagnostics;

namespace Catalog.BLL.Diagnostics;

public static class CatalogActivitySources
{
    public const string ProductsName = "Catalog.Products";

    public static readonly ActivitySource Products = new(ProductsName, "1.0.0");

    public static string[] SourceNames { get; } = [ProductsName];
}
