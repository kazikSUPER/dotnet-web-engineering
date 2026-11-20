using Orders.DAL.Data;
using Orders.DAL.Interfaces;
using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Orders.DAL.Repositories;

public class OrderRepository(OrdersDbContext context) : GenericRepository<Order>(context), IOrderRepository
{
    public Task<Order?> GetWithDetailsAsync(int id, CancellationToken ct = default) => Set.AsNoTracking().Include(x => x.Customer).Include(x => x.Items).Include(x => x.Payment).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Order?> GetWithExplicitPaymentAsync(int id, CancellationToken ct = default)
    {
        var order = await Set.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is not null) await Context.Entry(order).Reference(x => x.Payment).LoadAsync(ct);
        return order;
    }

    public async Task<Order?> GetWithExplicitStatusHistoryAsync(int id, CancellationToken ct = default)
    {
        var order = await Set.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is not null) await Context.Entry(order).Collection(x => x.StatusHistory).LoadAsync(ct);
        return order;
    }
}
