using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Orders.DAL.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(OrdersDbContext context, CancellationToken ct = default)
    {
        if (!await context.Customers.AnyAsync(ct))
        {
            await context.Customers.AddRangeAsync(new[]
            {
                new Customer("Олена", "Коваль", "olena.koval@example.com", "0501112233", 'F', 28),
                new Customer("Тарас", "Шевчук", "taras.shevchuk@example.com", "0673334455", 'M', 34)
            }, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
