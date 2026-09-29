using AutoMapper;
using Bakery.BLL.DTOs;
using Bakery.BLL.Interfaces;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Bakery.Domain.Exceptions;

namespace Bakery.BLL.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        return customer == null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByEmailAsync(email, ct);
        return customer == null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await _uow.Customers.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<CustomerDto>>(customers);
    }

    public async Task<CustomerDto> CreateAsync(CustomerCreateDto dto, CancellationToken ct = default)
    {
        // Бізнес-перевірка на унікальність Email
        var existing = await _uow.Customers.GetByEmailAsync(dto.Email, ct);
        if (existing != null)
        {
            throw new BusinessConflictException($"Клієнт з електронною поштою '{dto.Email}' вже зареєстрований.");
        }

        var customer = _mapper.Map<Customer>(dto);
        var id = await _uow.Customers.CreateAsync(customer, ct);
        customer.Id = id;

        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<bool> UpdateAsync(int id, CustomerUpdateDto dto, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer == null)
        {
            throw new NotFoundException(nameof(Customer), id);
        }

        // Перевірка, чи не зайнятий новий Email іншим користувачем
        var existingWithEmail = await _uow.Customers.GetByEmailAsync(dto.Email, ct);
        if (existingWithEmail != null && existingWithEmail.Id != id)
        {
            throw new BusinessConflictException($"Електронна пошта '{dto.Email}' вже використовується іншим клієнтом.");
        }

        _mapper.Map(dto, customer);
        return await _uow.Customers.UpdateAsync(customer, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer == null)
        {
            throw new NotFoundException(nameof(Customer), id);
        }

        return await _uow.Customers.DeleteAsync(id, ct);
    }
}
