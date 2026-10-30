using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bakery.DAL.Repositories;

public class PaymentRepository(BakeryDbContext context) : GenericRepository<Payment>(context), IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default) => Set.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
}
