using AutoMapper;
using Bakery.BLL.DTOs.Orders;
using Bakery.BLL.Interfaces;
using Bakery.BLL.Specifications;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Bakery.Domain.Exceptions;

namespace Bakery.BLL.Services;

public class OrderService(IUnitOfWork uow, IMapper mapper) : IOrderService
{
    public async Task<OrderResponseDto?> GetByIdAsync(int id, CancellationToken ct = default) => mapper.Map<OrderResponseDto>(await uow.Orders.GetWithDetailsAsync(id, ct));

    public async Task<PagedResponse<OrderResponseDto>> GetByCustomerIdAsync(int customerId, OrderQuery query, CancellationToken ct = default)
    {
        if (await uow.Customers.GetByIdAsync(customerId, ct) is null) throw new NotFoundException(nameof(Customer), customerId);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var specification = new CustomerOrdersSpecification(customerId, query.Status, page, pageSize, query.SortBy, query.Desc);
        var entities = await uow.Orders.ListAsync(specification, ct);
        var total = await uow.Orders.CountAsync(specification, ct);
        return new PagedResponse<OrderResponseDto> { Items = mapper.Map<IReadOnlyList<OrderResponseDto>>(entities), Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<OrderResponseDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        if (await uow.Customers.GetByIdAsync(request.CustomerId, ct) is null) throw new NotFoundException(nameof(Customer), request.CustomerId);
        var order = new Order(request.CustomerId, $"ORD-{Guid.NewGuid():N}"[..16], request.Notes);
        foreach (var item in request.Items)
        {
            var product = await uow.Products.GetByIdAsync(item.ProductId, ct) ?? throw new NotFoundException(nameof(Product), item.ProductId);
            order.AddItem(product, item.Quantity);
        }
        await uow.Orders.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<OrderResponseDto>(await uow.Orders.GetWithDetailsAsync(order.Id, ct));
    }

    public async Task UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await uow.Orders.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Order), id);
        order.ChangeStatus(request.NewStatus);
        uow.Orders.Update(order);
        await uow.SaveChangesAsync(ct);
    }

    public Task CancelAsync(int id, CancellationToken ct = default) => UpdateStatusAsync(id, new UpdateOrderStatusRequest { NewStatus = "Cancelled" }, ct);
}
