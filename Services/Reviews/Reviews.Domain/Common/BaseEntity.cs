using MongoDB.Bson;

namespace Bakery.Reviews.Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity()
    {
        Id = ObjectId.GenerateNewId();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        Version = 1;
    }

    public ObjectId Id { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public int Version { get; private set; }

    public void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void BumpVersion()
    {
        Version++;
        MarkUpdated();
    }

    protected void Touch()
    {
        MarkUpdated();
    }
}
