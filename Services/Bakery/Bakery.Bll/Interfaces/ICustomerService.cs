using Bakery.BLL.DTOs;

namespace Bakery.BLL.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CustomerCreateDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, CustomerUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
