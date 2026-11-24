using AutoMapper;
using Orders.BLL.Caching;
using Orders.BLL.DTOs.Customers;
using Orders.BLL.Interfaces;
using Orders.DAL.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;
using FluentValidation;

namespace Orders.BLL.Services;

public class CustomerService(IUnitOfWork uow, IMapper mapper, ICacheService cache, IValidator<CreateCustomerRequest> createValidator, IValidator<UpdateCustomerRequest> updateValidator) : ICustomerService
{
    public async Task<CustomerResponseDto?> GetByIdAsync(int id, CancellationToken ct = default) => await cache.GetOrCreateAsync(
        CacheKeys.Customer(id),
        async token => mapper.Map<CustomerResponseDto?>(await uow.Customers.GetByIdAsync(id, token)),
        l1Ttl: TimeSpan.FromSeconds(30),
        l2Ttl: TimeSpan.FromMinutes(5),
        ct);

    public async Task<IReadOnlyList<CustomerResponseDto>> GetAllAsync(CancellationToken ct = default) => await cache.GetOrCreateAsync(
        CacheKeys.CustomersList(),
        async token => mapper.Map<List<CustomerResponseDto>>(await uow.Customers.GetAllAsync(token)),
        l1Ttl: TimeSpan.FromSeconds(30),
        l2Ttl: TimeSpan.FromMinutes(5),
        ct);

    public async Task<CustomerResponseDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        if (await uow.Customers.GetByEmailAsync(request.Email, ct) is not null) throw new BusinessConflictException("Клієнт із таким Email уже існує.");
        var customer = new Customer(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Gender, request.Age);
        await uow.Customers.AddAsync(customer, ct);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.CustomersList(), "customer-created", ct);
        return mapper.Map<CustomerResponseDto>(customer);
    }

    public async Task UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);
        var customer = await uow.Customers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Customer), id);
        var duplicate = await uow.Customers.GetByEmailAsync(request.Email, ct);
        if (duplicate is not null && duplicate.Id != id) throw new BusinessConflictException("Клієнт із таким Email уже існує.");
        customer.Update(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Age);
        uow.Customers.Update(customer);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.Customer(id), "customer-updated", ct);
        await cache.RemoveAsync(CacheKeys.CustomersList(), "customer-updated", ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var customer = await uow.Customers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Customer), id);
        customer.Delete();
        uow.Customers.Update(customer);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.Customer(id), "customer-deleted", ct);
        await cache.RemoveAsync(CacheKeys.CustomersList(), "customer-deleted", ct);
    }
}
