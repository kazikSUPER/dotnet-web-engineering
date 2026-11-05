using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bakery.DAL.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(BakeryDbContext context, CancellationToken ct = default)
    {
        if (!await context.Products.AnyAsync(ct))
        {
            await context.Products.AddRangeAsync(new[]
            {
                new Product("Круасан класичний", 60m, 50),
                new Product("Багет французький", 40m, 40),
                new Product("Чізкейк Сан-Себастьян", 160m, 20)
            }, ct);
            await context.SaveChangesAsync(ct);
        }

        if (!await context.Customers.AnyAsync(ct))
        {
            await context.Customers.AddRangeAsync(new[]
            {
                new Customer("Олена", "Коваль", "olena.koval@example.com", "0501112233", 'F', 28),
                new Customer("Тарас", "Шевчук", "taras.shevchuk@example.com", "0673334455", 'M', 34)
            }, ct);
            await context.SaveChangesAsync(ct);
        }

        if (await context.Products.AnyAsync(ct) && !await context.ProductCategories.AnyAsync(ct))
        {
            var products = await context.Products.OrderBy(x => x.Id).ToListAsync(ct);
            var categories = await context.Categories.OrderBy(x => x.Id).ToListAsync(ct);
            var links = new List<ProductCategory>();
            foreach (var product in products)
            {
                for (var offset = 0; offset < 2 && categories.Count > 1; offset++)
                {
                    var category = categories[(product.Id + offset) % categories.Count];
                    if (links.Any(x => x.ProductId == product.Id && x.CategoryId == category.Id))
                    {
                        continue;
                    }
                    links.Add(new ProductCategory(product.Id, category.Id));
                }
            }
            await context.ProductCategories.AddRangeAsync(links, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
