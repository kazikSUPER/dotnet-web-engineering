using Orders.BLL.DTOs.Customers;

namespace Orders.BLL.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerResponseDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerResponseDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
