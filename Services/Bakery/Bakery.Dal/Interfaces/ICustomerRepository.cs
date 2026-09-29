using Bakery.Domain.Entities;

namespace Bakery.DAL.Interfaces;

/// <summary>
/// Інтерфейс репозиторію клієнтів (чистий ADO.NET).
/// Свідомо не наслідує IGenericRepository відповідно до критеріїв курсу.
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<int> CreateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
