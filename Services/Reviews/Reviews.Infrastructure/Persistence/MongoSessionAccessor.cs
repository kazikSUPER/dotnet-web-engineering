using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Persistence;

public interface IMongoSessionAccessor
{
    IClientSessionHandle? Current { get; }

    void Set(IClientSessionHandle? session);
}

public sealed class MongoSessionAccessor : IMongoSessionAccessor
{
    public IClientSessionHandle? Current { get; private set; }

    public void Set(IClientSessionHandle? session)
    {
        Current = session;
    }
}
