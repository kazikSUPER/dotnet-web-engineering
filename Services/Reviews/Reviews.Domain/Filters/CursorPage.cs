namespace Bakery.Reviews.Domain.Filters;

public sealed class CursorPage<T>
{
    public CursorPage(IReadOnlyList<T> items, string? nextCursor, bool hasMore)
    {
        Items = items;
        NextCursor = nextCursor;
        HasMore = hasMore;
    }

    public IReadOnlyList<T> Items { get; }

    public string? NextCursor { get; }

    public bool HasMore { get; }
}
