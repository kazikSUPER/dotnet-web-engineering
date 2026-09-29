using AutoMapper;
using Bakery.BLL.DTOs;
using Bakery.BLL.Interfaces;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Bakery.Domain.Exceptions;

namespace Bakery.BLL.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    // Прайс-лист для знімка цін товарів пекарні (Snapshot Pattern)
    private static readonly Dictionary<int, (string Name, decimal Price)> CatalogPriceSnapshot = new()
    {
        { 1, ("Круасан класичний масляний", 60.00m) },
        { 2, ("Багет традиційний французький", 40.00m) },
        { 3, ("Чізкейк Сан-Себастьян", 160.00m) },
        { 4, ("Хліб гречаний на заквасці", 55.00m) },
        { 5, ("Данська слойка з ягодами", 75.00m) }
    };

    public OrderService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<OrderResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _uow.Orders.GetWithDetailsAsync(id, ct);
        return order == null ? null : _mapper.Map<OrderResponseDto>(order);
    }

    public async Task<IReadOnlyList<OrderResponseDto>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(customerId, ct);
        if (customer == null)
        {
            throw new NotFoundException(nameof(Customer), customerId);
        }

        var orders = await _uow.Orders.GetByCustomerIdAsync(customerId, ct);
        return _mapper.Map<IReadOnlyList<OrderResponseDto>>(orders);
    }

    /// <summary>
    /// Транзакційна бізнес-операція створення замовлення:
    /// Змінює таблиці Orders та OrderItems у спільній транзакції Unit of Work з відкатом при помилці.
    /// Фіксує назву та ціну товару як знімок (Snapshot Pattern) з каталогу на момент покупки.
    /// </summary>
    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        // 1. Валідація існування клієнта
        var customer = await _uow.Customers.GetByIdAsync(dto.CustomerId, ct);
        if (customer == null)
        {
            throw new NotFoundException(nameof(Customer), dto.CustomerId);
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ValidationException(nameof(dto.Items), "Замовлення повинно містити щонайменше одну товарну позицію.");
        }

        // 2. Початок транзакції Unit of Work
        await _uow.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        try
        {
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = dto.CustomerId,
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedBy = customer.FullName
            };

            // Додаємо замовлення для отримання Id
            var orderId = await _uow.Orders.AddAsync(order, ct);
            decimal totalAmount = 0m;

            // 3. Формування товарних позицій зі знімком цін
            foreach (var itemDto in dto.Items)
            {
                if (!CatalogPriceSnapshot.TryGetValue(itemDto.ProductId, out var productInfo))
                {
                    throw new NotFoundException($"Товар з кодом ({itemDto.ProductId}) не знайдено в каталозі пекарні.");
                }

                if (itemDto.Quantity <= 0)
                {
                    throw new ValidationException(nameof(itemDto.Quantity), "Кількість товару повинна бути більшою за нуль.");
                }

                var orderItem = new OrderItem
                {
                    OrderId = orderId,
                    ProductId = itemDto.ProductId,
                    ProductName = productInfo.Name,    // Знімок назви
                    UnitPrice = productInfo.Price,     // Знімок ціни
                    Quantity = itemDto.Quantity
                };

                totalAmount += orderItem.TotalPrice;
                await _uow.Orders.AddOrderItemAsync(orderItem, ct);
                order.Items.Add(orderItem);
            }

            // 4. Оновлення загальної суми замовлення
            order.TotalAmount = totalAmount;
            await _uow.Orders.UpdateAsync(order, ct);

            // 5. Фіксація транзакції
            await _uow.CommitAsync(ct);

            order.Customer = customer;
            return _mapper.Map<OrderResponseDto>(order);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, CancellationToken ct = default)
    {
        var order = await _uow.Orders.GetByIdAsync(id, ct);
        if (order == null)
        {
            throw new NotFoundException(nameof(Order), id);
        }

        try
        {
            // Виклик транзакційної збережуваної процедури зі строгими бізнес-правилами
            return await _uow.Orders.UpdateStatusViaProcedureAsync(id, dto.NewStatus, dto.UpdatedBy, ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number >= 50000)
        {
            // Керована помилка зі збережуваної процедури (THROW) мапиться у доменний виняток
            throw new BusinessConflictException(ex.Message, ex);
        }
    }

    public async Task<bool> CancelOrderAsync(int id, CancellationToken ct = default)
    {
        return await UpdateStatusAsync(id, new UpdateOrderStatusDto
        {
            NewStatus = "Cancelled",
            UpdatedBy = "CustomerOrAdmin"
        }, ct);
    }
}
