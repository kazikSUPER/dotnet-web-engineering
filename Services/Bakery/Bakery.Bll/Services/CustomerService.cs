using AutoMapper;
using Bakery.BLL.DTOs.Customers;
using Bakery.BLL.Interfaces;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Bakery.Domain.Exceptions;

namespace Bakery.BLL.Services;

public class CustomerService(IUnitOfWork uow, IMapper mapper) : ICustomerService
{
    public async Task<CustomerResponseDto?> GetByIdAsync(int id, CancellationToken ct = default) => mapper.Map<CustomerResponseDto>(await uow.Customers.GetByIdAsync(id, ct));
    public async Task<IReadOnlyList<CustomerResponseDto>> GetAllAsync(CancellationToken ct = default) => mapper.Map<IReadOnlyList<CustomerResponseDto>>(await uow.Customers.GetAllAsync(ct));

    public async Task<CustomerResponseDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (await uow.Customers.GetByEmailAsync(request.Email, ct) is not null) throw new BusinessConflictException("Клієнт із таким Email уже існує.");
        var customer = new Customer(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Gender, request.Age);
        await uow.Customers.AddAsync(customer, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CustomerResponseDto>(customer);
    }

    public async Task UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await uow.Customers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Customer), id);
        var duplicate = await uow.Customers.GetByEmailAsync(request.Email, ct);
        if (duplicate is not null && duplicate.Id != id) throw new BusinessConflictException("Клієнт із таким Email уже існує.");
        customer.Update(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Age);
        uow.Customers.Update(customer);
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var customer = await uow.Customers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Customer), id);
        customer.Delete();
        uow.Customers.Update(customer);
        await uow.SaveChangesAsync(ct);
    }
}
