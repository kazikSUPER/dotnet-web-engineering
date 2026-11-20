using System.Diagnostics;

namespace Bakery.Reviews.Application.Diagnostics;

public static class ReviewsActivitySources
{
    public const string CommandsName = "Reviews.Commands";

    public static readonly ActivitySource Commands = new(CommandsName, "1.0.0");

    public static string[] SourceNames { get; } = [CommandsName];
}
