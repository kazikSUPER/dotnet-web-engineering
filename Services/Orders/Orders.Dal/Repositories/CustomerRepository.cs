using Orders.DAL.Data;
using Orders.DAL.Interfaces;
using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Orders.DAL.Repositories;

public class CustomerRepository(OrdersDbContext context) : GenericRepository<Customer>(context), ICustomerRepository
{
    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) => Set.AsNoTracking().SingleOrDefaultAsync(x => x.Email == email.ToLower(), ct);
}
