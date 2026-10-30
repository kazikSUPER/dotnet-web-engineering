namespace Bakery.Domain.Entities;

public class ProductCategory
{
    private ProductCategory() { }

    public ProductCategory(int productId, int categoryId)
    {
        ProductId = productId;
        CategoryId = categoryId;
        AddedAt = DateTime.UtcNow;
    }

    public int ProductId { get; private set; }
    public int CategoryId { get; private set; }
    public DateTime AddedAt { get; private set; }
    public Product? Product { get; private set; }
    public Category? Category { get; private set; }
}
