using System.Diagnostics;

namespace Platform.Aggregator.Diagnostics;

public static class AggregatorActivitySources
{
    public const string AssemblyName = "Aggregator.Assembly";

    public static readonly ActivitySource Assembly = new(AssemblyName);
}
