namespace Bakery.Reviews.Application.DTOs;

public sealed class ModerationLogDto
{
    public string Id { get; init; } = string.Empty;

    public string ReviewId { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string Moderator { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public DateTime Timestamp { get; init; }
}

public sealed class ModerationLogPageDto
{
    public IReadOnlyList<ModerationLogDto> Items { get; init; } = [];

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }
}
