using Orders.DAL.Data;
using Orders.DAL.Interfaces;
using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Orders.DAL.Repositories;

public class PaymentRepository(OrdersDbContext context) : GenericRepository<Payment>(context), IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default) => Set.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
}
