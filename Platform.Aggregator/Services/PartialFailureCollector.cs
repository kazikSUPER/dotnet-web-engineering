using System.Collections.Concurrent;
using Platform.Aggregator.Clients;

namespace Platform.Aggregator.Services;

internal sealed class PartialFailureCollector(ILogger logger)
{
    private readonly ConcurrentQueue<string> warnings = new();

    public int Count => warnings.Count;

    public IReadOnlyList<string> Snapshot() => warnings.ToArray();

    public void Add(string description) => warnings.Enqueue(description);

    public async Task<T?> CallAsync<T>(string description, Func<Task<T?>> call) where T : class
    {
        try
        {
            return await call();
        }
        catch (DownstreamServiceException exception)
        {
            warnings.Enqueue($"«{description}» недоступний: {exception.Reason}");

            logger.LogWarning(
                exception,
                "Агрегування продовжується без {Description}: сервіс {ServiceName} шлях {Path}",
                description,
                exception.ServiceName,
                exception.Path);

            return null;
        }
    }
}
