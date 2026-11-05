using MongoDB.Bson;

namespace Bakery.Reviews.Domain.Filters;

public readonly record struct CursorToken(long SortValue, ObjectId Id)
{
    public override string ToString() => $"{SortValue}_{Id}";

    public static CursorToken Create(DateTime createdAt, ObjectId id) => new(createdAt.Ticks, id);

    public static CursorToken Create(double averageRating, ObjectId id) => new(checked((long)Math.Round(averageRating * RatingPrecision)), id);

    public static double ToAverageRating(long sortValue) => sortValue / (double)RatingPrecision;

    public static DateTime ToCreatedAt(long sortValue) => new(sortValue, DateTimeKind.Utc);

    public static bool TryParse(string? value, out CursorToken token)
    {
        token = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('_');

        if (parts.Length != 2 || !long.TryParse(parts[0], out var sortValue) || !IsValidHex(parts[1]))
        {
            return false;
        }

        token = new CursorToken(sortValue, ObjectId.Parse(parts[1]));

        return true;
    }

    public static bool IsValid(string? value) => string.IsNullOrWhiteSpace(value) || TryParse(value, out _);

    private const int RatingPrecision = 1000;

    private static bool IsValidHex(string value) => value.Length == 24 && value.All(Uri.IsHexDigit);
}
