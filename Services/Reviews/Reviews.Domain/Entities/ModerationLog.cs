using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Exceptions;
using MongoDB.Bson;

namespace Bakery.Reviews.Domain.Entities;

public sealed class ModerationLog : BaseEntity
{
    private ModerationLog()
    {
        Moderator = string.Empty;
    }

    public ModerationLog(ObjectId reviewId, ModerationAction action, string moderator, string? notes = null)
    {
        var name = (moderator ?? string.Empty).Trim();

        if (name.Length is < 1 or > 50)
        {
            throw new DomainException("Ім'я модератора має бути від 1 до 50 символів.");
        }

        if (notes is { Length: > 250 })
        {
            throw new DomainException("Нотатка модератора не може перевищувати 250 символів.");
        }

        ReviewId = reviewId;
        Action = action;
        Moderator = name;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Timestamp = DateTime.UtcNow;
    }

    public ObjectId ReviewId { get; private set; }

    public ModerationAction Action { get; private set; }

    public string Moderator { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public DateTime Timestamp { get; private set; }
}
