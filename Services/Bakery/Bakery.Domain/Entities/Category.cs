namespace Bakery.Domain.Entities;

public class Category
{
    private Category() { }

    public Category(string name) => Name = name;

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<ProductCategory> ProductCategories { get; private set; } = new List<ProductCategory>();
}
