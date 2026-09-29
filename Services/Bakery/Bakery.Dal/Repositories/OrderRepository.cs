using System.Data;
using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Dapper;

namespace Bakery.DAL.Repositories;

public class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public OrderRepository(IDbConnectionFactory connectionFactory, IDbConnection connection, IDbTransaction? transaction = null)
        : base(connectionFactory, "dbo.Orders")
    {
        _connection = connection;
        _transaction = transaction;
    }

    public override async Task<int> AddAsync(Order entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Orders (OrderNumber, CustomerId, Status, TotalAmount, OrderDate, Notes, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
            VALUES (@OrderNumber, @CustomerId, @Status, @TotalAmount, @OrderDate, @Notes, SYSUTCDATETIME(), @CreatedBy, SYSUTCDATETIME(), @CreatedBy, 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        var id = await _connection.ExecuteScalarAsync<int>(cmd);
        entity.Id = id;
        return id;
    }

    public override async Task<bool> UpdateAsync(Order entity, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Orders
            SET Status = @Status,
                TotalAmount = @TotalAmount,
                Notes = @Notes,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND IsDeleted = 0";

        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        var affected = await _connection.ExecuteAsync(cmd);
        return affected > 0;
    }

    /// <summary>
    /// Multi-mapping запит: повертає замовлення разом із клієнтом та товарними позиціями
    /// </summary>
    public async Task<Order?> GetWithDetailsAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                o.Id, o.OrderNumber, o.CustomerId, o.Status, o.TotalAmount, o.OrderDate, o.Notes, o.CreatedAt, o.IsDeleted,
                c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.Gender, c.Age, c.CreatedAt, c.IsDeleted,
                i.Id, i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice, i.CreatedAt, i.IsDeleted
            FROM dbo.Orders o
            INNER JOIN dbo.Customers c ON o.CustomerId = c.Id
            LEFT JOIN dbo.OrderItems i ON o.Id = i.OrderId AND i.IsDeleted = 0
            WHERE o.Id = @Id AND o.IsDeleted = 0";

        var orderDictionary = new Dictionary<int, Order>();

        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: _transaction, cancellationToken: ct);
        await _connection.QueryAsync<Order, Customer, OrderItem?, Order>(
            cmd,
            (order, customer, item) =>
            {
                if (!orderDictionary.TryGetValue(order.Id, out var currentOrder))
                {
                    currentOrder = order;
                    currentOrder.Customer = customer;
                    currentOrder.Items = new List<OrderItem>();
                    orderDictionary.Add(currentOrder.Id, currentOrder);
                }

                if (item != null && !currentOrder.Items.Any(x => x.Id == item.Id))
                {
                    currentOrder.Items.Add(item);
                }

                return currentOrder;
            },
            splitOn: "Id,Id"
        );

        return orderDictionary.Values.FirstOrDefault();
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM dbo.Orders 
            WHERE CustomerId = @CustomerId AND IsDeleted = 0 
            ORDER BY OrderDate DESC";

        var cmd = new CommandDefinition(sql, new { CustomerId = customerId }, transaction: _transaction, cancellationToken: ct);
        var result = await _connection.QueryAsync<Order>(cmd);
        return result.ToList();
    }

    /// <summary>
    /// Виклик збережуваної процедури транзакційної зміни статусу через Dapper
    /// </summary>
    public async Task<bool> UpdateStatusViaProcedureAsync(int orderId, string newStatus, string updatedBy, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@OrderId", orderId, DbType.Int32);
        parameters.Add("@NewStatus", newStatus, DbType.String, size: 20);
        parameters.Add("@UpdatedBy", updatedBy, DbType.String, size: 50);

        var cmd = new CommandDefinition(
            "dbo.sp_UpdateOrderStatus",
            parameters,
            transaction: _transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct
        );

        await _connection.ExecuteAsync(cmd);
        return true;
    }

    public async Task AddOrderItemAsync(OrderItem item, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
            VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Quantity, SYSUTCDATETIME(), 'System', SYSUTCDATETIME(), 'System', 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var cmd = new CommandDefinition(sql, item, transaction: _transaction, cancellationToken: ct);
        var id = await _connection.ExecuteScalarAsync<int>(cmd);
        item.Id = id;
    }
}
