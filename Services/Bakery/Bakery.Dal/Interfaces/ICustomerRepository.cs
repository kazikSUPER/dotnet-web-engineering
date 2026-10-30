using Bakery.Domain.Entities;

namespace Bakery.DAL.Interfaces;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
}
