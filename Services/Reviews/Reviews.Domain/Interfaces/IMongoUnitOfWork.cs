namespace Bakery.Reviews.Domain.Interfaces;

public interface IMongoUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default);
}
