namespace Bakery.Domain.Entities;

public class ProductImage
{
    private ProductImage() { }

    public ProductImage(int productId, string url, int sortOrder)
    {
        ProductId = productId;
        Url = url;
        SortOrder = sortOrder;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public Product? Product { get; private set; }
}
