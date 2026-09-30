using System.Data;
using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Dapper;

namespace Bakery.DAL.Repositories;

public class OrderStatusHistoryRepository : BaseDapperRepository<OrderStatusHistory>, IOrderStatusHistoryRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public OrderStatusHistoryRepository(IDbConnectionFactory connectionFactory, IDbConnection connection, IDbTransaction? transaction = null)
        : base(connectionFactory, "dbo.OrderStatusHistory")
    {
        _connection = connection;
        _transaction = transaction;
    }

    public override async Task<int> AddAsync(OrderStatusHistory entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.OrderStatusHistory (OrderId, Status, ChangedAt, Comment)
            VALUES (@OrderId, @Status, @ChangedAt, @Comment);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        return await _connection.ExecuteScalarAsync<int>(cmd);
    }

    public override async Task<bool> UpdateAsync(OrderStatusHistory entity, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.OrderStatusHistory
            SET Status = @Status, Comment = @Comment
            WHERE Id = @Id;";

        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        var rows = await _connection.ExecuteAsync(cmd);
        return rows > 0;
    }

    public async Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, OrderId, Status, ChangedAt, Comment FROM dbo.OrderStatusHistory WHERE OrderId = @OrderId ORDER BY ChangedAt;";
        var cmd = new CommandDefinition(sql, new { OrderId = orderId }, transaction: _transaction, cancellationToken: ct);
        return await _connection.QueryAsync<OrderStatusHistory>(cmd);
    }
}
