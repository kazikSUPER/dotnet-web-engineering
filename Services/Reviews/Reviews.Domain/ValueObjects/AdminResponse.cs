using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Exceptions;

namespace Bakery.Reviews.Domain.ValueObjects;

public sealed class AdminResponse : ValueObject
{
    public const int MinMessageLength = 1;
    public const int MaxMessageLength = 500;

    private AdminResponse()
    {
        ModeratorId = null!;
        Message = null!;
    }

    public AdminResponse(string moderatorId, string message, DateTime respondedAt)
    {
        var moderator = (moderatorId ?? string.Empty).Trim();

        if (moderator.Length is < 1 or > 50)
        {
            throw new DomainException("Ідентифікатор модератора має бути від 1 до 50 символів.");
        }

        var text = (message ?? string.Empty).Trim();

        if (text.Length is < MinMessageLength or > MaxMessageLength)
        {
            throw new DomainException($"Повідомлення адміністратора має бути від {MinMessageLength} до {MaxMessageLength} символів.");
        }

        ModeratorId = moderator;
        Message = text;
        RespondedAt = respondedAt == default ? DateTime.UtcNow : respondedAt.ToUniversalTime();
    }

    public string ModeratorId { get; }

    public string Message { get; }

    public DateTime RespondedAt { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ModeratorId;
        yield return Message;
        yield return RespondedAt;
    }
}
