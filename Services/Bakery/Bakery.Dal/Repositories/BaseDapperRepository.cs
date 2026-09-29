using System.Data;
using Bakery.DAL.Data;
using Bakery.Domain.Interfaces;
using Dapper;

namespace Bakery.DAL.Repositories;

public abstract class BaseDapperRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly string TableName;

    protected BaseDapperRepository(IDbConnectionFactory connectionFactory, string tableName)
    {
        ConnectionFactory = connectionFactory;
        TableName = tableName;
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var connection = ConnectionFactory.CreateConnection();
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id AND IsDeleted = 0";
        var cmd = new CommandDefinition(sql, new { Id = id }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<T>(cmd);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = ConnectionFactory.CreateConnection();
        var sql = $"SELECT * FROM {TableName} WHERE IsDeleted = 0 ORDER BY Id DESC";
        var cmd = new CommandDefinition(sql, cancellationToken: ct);
        var result = await connection.QueryAsync<T>(cmd);
        return result.ToList();
    }

    public abstract Task<int> AddAsync(T entity, CancellationToken ct = default);
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct = default);

    public virtual async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var connection = ConnectionFactory.CreateConnection();
        var sql = $"UPDATE {TableName} SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, cancellationToken: ct);
        var rowsAffected = await connection.ExecuteAsync(cmd);
        return rowsAffected > 0;
    }
}
