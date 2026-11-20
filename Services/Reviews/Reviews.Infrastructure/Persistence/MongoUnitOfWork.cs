using Bakery.Reviews.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Persistence;

public sealed class MongoTransactionState(ILogger<MongoTransactionState> logger)
{
    public bool Supported { get; private set; } = true;

    public void Disable(Exception exception)
    {
        Supported = false;

        logger.LogWarning(
            exception,
            "Сервер MongoDB не підтримує транзакції — далі операції виконуються без транзакційного блоку");
    }
}

public sealed class MongoUnitOfWork(
    IMongoClient client,
    IOptions<MongoDbOptions> options,
    IMongoSessionAccessor sessions,
    MongoTransactionState transactions,
    ILogger<MongoUnitOfWork> logger) : IMongoUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default)
    {
        if (sessions.Current is not null)
        {
            logger.LogDebug("Сесія вже активна — роботу виконано в межах поточної транзакції");
            await work(ct);

            return;
        }

        var useTransaction = options.Value.UseTransactions && transactions.Supported;

        using var session = useTransaction ? client.StartSession() : null;

        sessions.Set(session);

        var transactionStarted = false;

        try
        {
            if (session is null)
            {
                await work(ct);

                return;
            }

            try
            {
                session.StartTransaction();
                transactionStarted = true;
            }
            catch (MongoCommandException exception)
            {
                transactions.Disable(exception);
                await work(ct);

                return;
            }

            await work(ct);
            await session.CommitTransactionAsync(ct);
        }
        catch
        {
            if (transactionStarted && session is not null)
            {
                await session.AbortTransactionAsync(ct);
            }

            throw;
        }
        finally
        {
            sessions.Set(null);
        }
    }
}
