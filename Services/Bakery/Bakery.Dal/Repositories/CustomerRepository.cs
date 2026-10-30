using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bakery.DAL.Repositories;

public class CustomerRepository(BakeryDbContext context) : GenericRepository<Customer>(context), ICustomerRepository
{
    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) => Set.AsNoTracking().SingleOrDefaultAsync(x => x.Email == email.ToLower(), ct);
}
