using System.Data;
using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Dapper;

namespace Bakery.DAL.Repositories;

public class PaymentRepository : BaseDapperRepository<Payment>, IPaymentRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public PaymentRepository(IDbConnectionFactory connectionFactory, IDbConnection connection, IDbTransaction? transaction = null)
        : base(connectionFactory, "dbo.Payments")
    {
        _connection = connection;
        _transaction = transaction;
    }

    public override async Task<Payment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await GetByOrderIdAsync(id, ct);
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM dbo.Payments 
            WHERE OrderId = @OrderId AND IsDeleted = 0";

        var cmd = new CommandDefinition(sql, new { OrderId = orderId }, transaction: _transaction, cancellationToken: ct);
        return await _connection.QuerySingleOrDefaultAsync<Payment>(cmd);
    }

    public async Task<Payment?> GetByTransactionNumberAsync(string transactionNumber, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM dbo.Payments 
            WHERE TransactionNumber = @TransactionNumber AND IsDeleted = 0";

        var cmd = new CommandDefinition(sql, new { TransactionNumber = transactionNumber }, transaction: _transaction, cancellationToken: ct);
        return await _connection.QuerySingleOrDefaultAsync<Payment>(cmd);
    }

    public override async Task<int> AddAsync(Payment entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Payments (OrderId, TransactionNumber, PaymentMethod, Amount, PaymentDate, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
            VALUES (@OrderId, @TransactionNumber, @PaymentMethod, @Amount, @PaymentDate, @Status, SYSUTCDATETIME(), @CreatedBy, SYSUTCDATETIME(), @CreatedBy, 0);";

        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        await _connection.ExecuteAsync(cmd);
        return entity.OrderId;
    }

    public override async Task<bool> UpdateAsync(Payment entity, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Payments
            SET Status = @Status,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedBy = @UpdatedBy
            WHERE OrderId = @OrderId AND IsDeleted = 0";

        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        var affected = await _connection.ExecuteAsync(cmd);
        return affected > 0;
    }

    public async Task<bool> ProcessPaymentViaProcedureAsync(int orderId, string transactionNumber, string paymentMethod, decimal amount, string processedBy, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@OrderId", orderId, DbType.Int32);
        parameters.Add("@TransactionNumber", transactionNumber, DbType.String, size: 50);
        parameters.Add("@PaymentMethod", paymentMethod, DbType.String, size: 30);
        parameters.Add("@Amount", amount, DbType.Decimal);
        parameters.Add("@ProcessedBy", processedBy, DbType.String, size: 50);

        var cmd = new CommandDefinition(
            "dbo.sp_ProcessPayment",
            parameters,
            transaction: _transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct
        );

        await _connection.ExecuteAsync(cmd);
        return true;
    }
}
