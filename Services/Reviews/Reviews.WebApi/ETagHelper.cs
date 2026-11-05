namespace Bakery.Reviews.WebApi;

internal static class ETagHelper
{
    public static string Format(int version) => $"W/\"{version}\"";

    public static int? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "*")
        {
            return null;
        }

        var candidate = value.Trim();

        if (candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[2..];
        }

        candidate = candidate.Trim('"');

        return int.TryParse(candidate, out var version) ? version : null;
    }
}
