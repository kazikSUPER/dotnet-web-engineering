namespace Catalog.BLL.DTOs.Products;

public sealed class ProductCardDto
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public bool IsAvailable { get; init; }
    public string? Description { get; init; }
    public string? Manufacturer { get; init; }
    public string[] Categories { get; init; } = [];
}
