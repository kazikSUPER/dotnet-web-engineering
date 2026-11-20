using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace Platform.ServiceDefaults;

public static class MongoDbInstrumentation
{
    public static string ActivitySourceName => DiagnosticsActivityEventSubscriber.ActivitySourceName;

    public static MongoClientSettings WithOpenTelemetry(this MongoClientSettings settings)
    {
        settings.ClusterConfigurator = builder => builder.Subscribe(
            new DiagnosticsActivityEventSubscriber(new InstrumentationOptions { CaptureCommandText = true }));

        return settings;
    }
}
