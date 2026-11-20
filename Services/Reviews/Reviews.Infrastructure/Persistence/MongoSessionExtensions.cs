using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Persistence;

internal static class MongoSessionExtensions
{
    public static IFindFluent<TDocument, TDocument> Find<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        FilterDefinition<TDocument> filter)
    {
        return collection.Find(session, filter, null);
    }

    public static IAggregateFluent<TDocument> Aggregate<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session)
    {
        return collection.Aggregate(session, null);
    }

    public static Task InsertOneAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        TDocument document,
        CancellationToken ct)
    {
        return collection.InsertOneAsync(session, document, null, ct);
    }

    public static Task InsertManyAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        IEnumerable<TDocument> documents,
        CancellationToken ct)
    {
        return collection.InsertManyAsync(session, documents, null, ct);
    }

    public static Task<UpdateResult> UpdateOneAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        CancellationToken ct)
    {
        return collection.UpdateOneAsync(session, filter, update, null, ct);
    }

    public static Task<UpdateResult> UpdateOneAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        UpdateOptions options,
        CancellationToken ct)
    {
        return collection.UpdateOneAsync(session, filter, update, options, ct);
    }

    public static Task<UpdateResult> UpdateManyAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        CancellationToken ct)
    {
        return collection.UpdateManyAsync(session, filter, update, null, ct);
    }

    public static Task<DeleteResult> DeleteOneAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        FilterDefinition<TDocument> filter,
        CancellationToken ct)
    {
        return collection.DeleteOneAsync(session, filter, null, ct);
    }
}
