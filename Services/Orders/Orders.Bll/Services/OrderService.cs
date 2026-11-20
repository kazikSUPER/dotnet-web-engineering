using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Orders.BLL.Caching;
using Orders.BLL.Diagnostics;
using Orders.BLL.DTOs.Orders;
using Orders.BLL.Interfaces;
using Orders.BLL.Specifications;
using Orders.DAL.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;
using Platform.EventBus.Events.Contracts;
using Platform.EventBus.Publishing;
using System.Diagnostics;

namespace Orders.BLL.Services;

public class OrderService(
    IUnitOfWork uow,
    IMapper mapper,
    ICacheService cache,
    IValidator<CreateOrderRequest> createValidator,
    IValidator<UpdateOrderStatusRequest> statusValidator,
    IEventPublisher eventPublisher,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderResponseDto?> GetByIdAsync(int id, CancellationToken ct = default) => await cache.GetOrCreateAsync(
        CacheKeys.Order(id),
        async token => mapper.Map<OrderResponseDto?>(await uow.Orders.GetWithDetailsAsync(id, token)),
        l1Ttl: TimeSpan.FromMinutes(2),
        l2Ttl: TimeSpan.FromMinutes(5),
        ct);

    public async Task<IReadOnlyList<OrderStatusHistoryDto>> GetStatusHistoryAsync(int id, CancellationToken ct = default) => await cache.GetOrCreateAsync(
        CacheKeys.OrderHistory(id),
        async token =>
        {
            var order = await uow.Orders.GetWithExplicitStatusHistoryAsync(id, token) ?? throw new NotFoundException(nameof(Order), id);
            return mapper.Map<List<OrderStatusHistoryDto>>(order.StatusHistory.OrderBy(x => x.ChangedAt).ToList());
        },
        l1Ttl: TimeSpan.FromMinutes(2),
        l2Ttl: TimeSpan.FromMinutes(5),
        ct);

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
        using var activity = OrdersActivitySources.Orders.StartActivity("order.create", ActivityKind.Internal);
        await createValidator.ValidateAndThrowAsync(request, ct);
        if (await uow.Customers.GetByIdAsync(request.CustomerId, ct) is null) throw new NotFoundException(nameof(Customer), request.CustomerId);
        var order = new Order(request.CustomerId, $"ORD-{Guid.NewGuid():N}"[..16], request.Notes);
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }
        await uow.Orders.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        var created = await uow.Orders.GetWithDetailsAsync(order.Id, ct);

        // 6. Publish event ONLY after successful SaveChanges
        try
        {
            await eventPublisher.PublishAsync(new OrderPlacedEvent(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.TotalAmount), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish OrderPlacedEvent for order {OrderId}", order.Id);
        }

        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.number", order.OrderNumber);
        activity?.SetTag("order.customer_id", order.CustomerId);
        activity?.SetTag("order.status", order.Status);
        activity?.SetTag("order.total_amount", order.TotalAmount);

        logger.LogInformation(
            "Створено замовлення {OrderId} {OrderNumber} для клієнта {CustomerId}: {ItemCount} позицій на {TotalAmount}, статус {Status}",
            order.Id, order.OrderNumber, order.CustomerId, order.Items.Count, order.TotalAmount, order.Status);
        return mapper.Map<OrderResponseDto>(created);
    }

    public async Task UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        using var activity = OrdersActivitySources.Orders.StartActivity("order.status.update", ActivityKind.Internal);
        await statusValidator.ValidateAndThrowAsync(request, ct);
        var order = await uow.Orders.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Order), id);
        var previousStatus = order.Status;
        order.ChangeStatus(request.NewStatus);
        uow.Orders.Update(order);
        await uow.SaveChangesAsync(ct);

        // 6. Publish event ONLY after successful SaveChanges
        try
        {
            await eventPublisher.PublishAsync(new OrderStatusChangedEvent(
                order.Id,
                previousStatus,
                order.Status), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish OrderStatusChangedEvent for order {OrderId}", order.Id);
        }

        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.status.previous", previousStatus);
        activity?.SetTag("order.status", order.Status);

        logger.LogInformation(
            "Замовлення {OrderId} змінило статус {PreviousStatus} → {Status}",
            order.Id, previousStatus, order.Status);

        var reason = $"order-status:{request.NewStatus}";
        await cache.RemoveAsync(CacheKeys.Order(id), reason, ct);
        await cache.RemoveAsync(CacheKeys.OrderHistory(id), reason, ct);
        await cache.RemoveAsync(CacheKeys.AggregateOrderFull(id), reason, ct);
    }

    public Task CancelAsync(int id, CancellationToken ct = default) => UpdateStatusAsync(id, new UpdateOrderStatusRequest { NewStatus = "Cancelled" }, ct);
}