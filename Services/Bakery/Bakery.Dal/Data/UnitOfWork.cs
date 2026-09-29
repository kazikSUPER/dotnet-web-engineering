using System.Data;
using Bakery.DAL.Interfaces;
using Bakery.DAL.Repositories;
using Microsoft.Data.SqlClient;

namespace Bakery.DAL.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private readonly IDbConnectionFactory _connectionFactory;
    private SqlTransaction? _transaction;
    private bool _disposed;

    private ICustomerRepository? _customers;
    private IOrderRepository? _orders;
    private IPaymentRepository? _payments;

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _connection = (SqlConnection)connectionFactory.CreateConnection();
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    public ICustomerRepository Customers => 
        _customers ??= new CustomerRepository(_connection, _transaction);

    public IOrderRepository Orders => 
        _orders ??= new OrderRepository(_connectionFactory, _connection, _transaction);

    public IPaymentRepository Payments => 
        _payments ??= new PaymentRepository(_connectionFactory, _connection, _transaction);

    public Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Транзакція вже активна в цьому Unit of Work.");
        }

        _transaction = _connection.BeginTransaction(isolationLevel);
        
        // Оновлюємо посилання репозиторіїв на нову транзакцію
        _customers = new CustomerRepository(_connection, _transaction);
        _orders = new OrderRepository(_connectionFactory, _connection, _transaction);
        _payments = new PaymentRepository(_connectionFactory, _connection, _transaction);

        return Task.FromResult<IDbTransaction>(_transaction);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Немає активної транзакції для фіксації (Commit).");
        }

        try
        {
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            ResetRepositories();
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            ResetRepositories();
        }
    }

    private void ResetRepositories()
    {
        _customers = new CustomerRepository(_connection, null);
        _orders = new OrderRepository(_connectionFactory, _connection, null);
        _payments = new PaymentRepository(_connectionFactory, _connection, null);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
