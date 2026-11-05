namespace Bakery.BLL.DTOs.Products;

public sealed class ProductInCategoryDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public DateTime AddedAt { get; init; }
}
