using System.Data;
using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Dapper;

namespace Bakery.DAL.Repositories;

public class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public ProductRepository(IDbConnectionFactory connectionFactory, IDbConnection connection, IDbTransaction? transaction = null)
        : base(connectionFactory, "dbo.Products")
    {
        _connection = connection;
        _transaction = transaction;
    }

    public override async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Name, Price, IsAvailable FROM dbo.Products WHERE Id = @Id;";
        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: _transaction, cancellationToken: ct);
        return await _connection.QuerySingleOrDefaultAsync<Product>(cmd);
    }

    public override async Task<int> AddAsync(Product entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Products (Name, Price, IsAvailable)
            VALUES (@Name, @Price, @IsAvailable);
            SELECT CAST(SCOPE_IDENTITY() as int);";
        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        return await _connection.ExecuteScalarAsync<int>(cmd);
    }

    public override async Task<bool> UpdateAsync(Product entity, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Products 
            SET Name = @Name, Price = @Price, IsAvailable = @IsAvailable 
            WHERE Id = @Id;";
        var cmd = new CommandDefinition(sql, entity, transaction: _transaction, cancellationToken: ct);
        var rows = await _connection.ExecuteAsync(cmd);
        return rows > 0;
    }
}
