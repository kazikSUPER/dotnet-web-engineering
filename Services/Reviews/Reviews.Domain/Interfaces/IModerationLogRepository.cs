using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Filters;
using MongoDB.Bson;

namespace Bakery.Reviews.Domain.Interfaces;

public interface IModerationLogRepository
{
    Task AddAsync(ModerationLog log, CancellationToken ct = default);

    Task<bool> AnyAsync(CancellationToken ct = default);

    Task<CursorPage<ModerationLog>> GetPageAsync(ObjectId? reviewId, string? cursor, int limit, CancellationToken ct = default);
}
