using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Product
{
    private Product() { }

    public Product(string name, decimal price, int stock = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainValidationException("Назва товару є обов'язковою.");
        if (price <= 0) throw new DomainValidationException("Ціна товару має бути більшою за нуль.");
        if (stock < 0) throw new DomainValidationException("Залишок не може бути від'ємним.");
        Name = name.Trim();
        Price = price;
        Stock = stock;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public double AverageRating { get; private set; }
    public int ReviewsCount { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public bool IsAvailable => Stock > 0;
    public ICollection<ProductCategory> ProductCategories { get; private set; } = new List<ProductCategory>();
    public ProductDetail? Detail { get; private set; }
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    public void UpdateRating(double averageRating, int reviewsCount)
    {
        AverageRating = averageRating;
        ReviewsCount = reviewsCount;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new DomainValidationException("Назва товару є обов'язковою.");
        Name = newName.Trim();
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new DomainValidationException("Ціна товару має бути більшою за нуль.");
        Price = newPrice;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0) throw new DomainValidationException("Кількість для додавання має бути більшою за нуль.");
        Stock += quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0 || Stock < quantity) throw new DomainValidationException("Недостатній залишок товару.");
        Stock -= quantity;
    }
}
