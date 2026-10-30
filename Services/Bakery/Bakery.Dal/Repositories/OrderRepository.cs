using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bakery.DAL.Repositories;

public class OrderRepository(BakeryDbContext context) : GenericRepository<Order>(context), IOrderRepository
{
    public Task<Order?> GetWithDetailsAsync(int id, CancellationToken ct = default) => Set.AsNoTracking().Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.Product).Include(x => x.Payment).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Order?> GetWithExplicitPaymentAsync(int id, CancellationToken ct = default)
    {
        var order = await Set.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is not null) await Context.Entry(order).Reference(x => x.Payment).LoadAsync(ct);
        return order;
    }
}
