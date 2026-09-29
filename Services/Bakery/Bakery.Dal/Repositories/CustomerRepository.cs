using System.Data;
using System.Data.Common;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Bakery.DAL.Repositories;

/// <summary>
/// Репозиторій клієнтів на чистому ADO.NET (SqlConnection / SqlCommand / SqlDataReader).
/// Свідомо НЕ успадковує IGenericRepository відповідно до архітектурних вимог курсу.
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public CustomerRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    private SqlCommand CreateCommand(string sql)
    {
        var sqlConn = (SqlConnection)_connection;
        if (sqlConn.State != ConnectionState.Open)
        {
            sqlConn.Open();
        }

        var cmd = sqlConn.CreateCommand();
        cmd.CommandText = sql;
        if (_transaction is SqlTransaction sqlTx)
        {
            cmd.Transaction = sqlTx;
        }
        return cmd;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedAt, IsDeleted
            FROM dbo.Customers
            WHERE Id = @Id AND IsDeleted = 0";

        await using var cmd = CreateCommand(sql);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
        if (await reader.ReadAsync(ct))
        {
            return MapCustomerFromReader(reader);
        }

        return null;
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedAt, IsDeleted
            FROM dbo.Customers
            WHERE Email = @Email AND IsDeleted = 0";

        await using var cmd = CreateCommand(sql);
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = email });

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
        if (await reader.ReadAsync(ct))
        {
            return MapCustomerFromReader(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedAt, IsDeleted
            FROM dbo.Customers
            WHERE IsDeleted = 0
            ORDER BY LastName, FirstName";

        await using var cmd = CreateCommand(sql);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Customer>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapCustomerFromReader(reader));
        }

        return list;
    }

    public async Task<int> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Customers (FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
            VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @Gender, @Age, SYSUTCDATETIME(), 'System', SYSUTCDATETIME(), 'System', 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var cmd = CreateCommand(sql);
        cmd.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 50) { Value = customer.FirstName });
        cmd.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 50) { Value = customer.LastName });
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = customer.Email });
        cmd.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = customer.PhoneNumber });
        cmd.Parameters.Add(new SqlParameter("@Gender", SqlDbType.Char, 1) { Value = customer.Gender });
        cmd.Parameters.Add(new SqlParameter("@Age", SqlDbType.Int) { Value = customer.Age });

        var scalarResult = await cmd.ExecuteScalarAsync(ct);
        var newId = Convert.ToInt32(scalarResult);
        customer.Id = newId;
        return newId;
    }

    public async Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customers
            SET FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                PhoneNumber = @PhoneNumber,
                Gender = @Gender,
                Age = @Age,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND IsDeleted = 0";

        await using var cmd = CreateCommand(sql);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = customer.Id });
        cmd.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 50) { Value = customer.FirstName });
        cmd.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 50) { Value = customer.LastName });
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = customer.Email });
        cmd.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = customer.PhoneNumber });
        cmd.Parameters.Add(new SqlParameter("@Gender", SqlDbType.Char, 1) { Value = customer.Gender });
        cmd.Parameters.Add(new SqlParameter("@Age", SqlDbType.Int) { Value = customer.Age });

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customers
            SET IsDeleted = 1,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id";

        await using var cmd = CreateCommand(sql);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    private static Customer MapCustomerFromReader(DbDataReader reader)
    {
        return new Customer
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
            Gender = reader.GetString(reader.GetOrdinal("Gender"))[0],
            Age = reader.GetInt32(reader.GetOrdinal("Age")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
        };
    }
}
