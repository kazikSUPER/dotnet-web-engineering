namespace Bakery.Domain.Entities;

public class ProductDetail
{
    private ProductDetail() { }

    public ProductDetail(int productId, string description, string? manufacturer)
    {
        ProductId = productId;
        Description = description;
        Manufacturer = manufacturer;
    }

    public int ProductId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Manufacturer { get; private set; }
    public Product? Product { get; private set; }
}
