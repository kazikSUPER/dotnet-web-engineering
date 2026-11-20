using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Diagnostics;

namespace Platform.ServiceDefaults;

public static class Extensions
{
    public const string HealthEndpointPath = "/health";
    public const string AliveEndpointPath = "/alive";

    public static readonly ActivitySource PlatformActivitySource = new("Platform");

    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder,
        params string[] activitySourceNames)
    {
        builder.ConfigureOpenTelemetry(activitySourceNames);
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddTransient<CorrelationIdPropagationHandler>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddServiceDiscovery();
            http.AddHttpMessageHandler<CorrelationIdPropagationHandler>();
            http.AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.DisableForUnsafeHttpMethods();
                options.Retry.OnRetry = args =>
                {
                    Log.Warning("HTTP retry attempt {AttemptNumber} after {RetryDelay}ms", args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                };

                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 10;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
                options.CircuitBreaker.OnOpened = args =>
                {
                    Log.Error("Standard circuit breaker opened. Break duration: {BreakDuration}s", args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                };
                options.CircuitBreaker.OnClosed = args =>
                {
                    Log.Information("Standard circuit breaker closed");
                    return ValueTask.CompletedTask;
                };
                options.CircuitBreaker.OnHalfOpened = args =>
                {
                    Log.Information("Standard circuit breaker half-opened");
                    return ValueTask.CompletedTask;
                };

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
            });
        });

        return builder;
    }

    public static Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions DisableForUnsafeHttpMethods(
        this Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions options)
    {
        var originalShouldHandle = options.ShouldHandle;
        options.ShouldHandle = args =>
        {
            if (args.Outcome.Result?.RequestMessage?.Method is { } method &&
                (method == HttpMethod.Post || method == HttpMethod.Patch || method == HttpMethod.Delete))
            {
                return ValueTask.FromResult(false);
            }

            return originalShouldHandle?.Invoke(args) ?? ValueTask.FromResult(false);
        };
        return options;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(
        this IHostApplicationBuilder builder,
        params string[] activitySourceNames)
    {
        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: builder.Environment.ApplicationName,
                serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
                serviceInstanceId: Environment.MachineName))
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddMeter("Orders.Api")
                .AddMeter("Catalog.Api")
                .AddMeter("Reviews.WebApi")
                .AddMeter("Platform.Aggregator")
                .AddMeter("Platform.Caching")
                .AddMeter("Polly"))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options => options.Filter = IsHealthRequest)
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddSource(MongoDbInstrumentation.ActivitySourceName)
                    .AddSource(PlatformActivitySource.Name)
                    .AddSource("Grpc.Net.Client")
                    .AddSource("grpc.server");

                foreach (var sourceName in activitySourceNames.Distinct(StringComparer.Ordinal))
                {
                    tracing.AddSource(sourceName);
                }
            });

        if (hasOtlpEndpoint)
        {
            openTelemetry.UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    public static IHostBuilder UsePlatformSerilog(this IHostBuilder builder, string serviceName)
    {
        return builder.UseSerilog(
            (context, services, configuration) => configuration
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .MinimumLevel.Is(context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("MongoDB", LogEventLevel.Warning)
                .WriteTo.Console(new CompactJsonFormatter())
                .ReadFrom.Services(services)
                .ReadFrom.Configuration(context.Configuration),
            preserveStaticLogger: false,
            writeToProviders: true);
    }

    public static WebApplication UsePlatformDefaults(this WebApplication app, string serviceName)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} answered {StatusCode} in {Elapsed:0.0} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("ServiceName", serviceName);
                diagnosticContext.Set("CorrelationId", CorrelationIdMiddleware.Of(httpContext));
            };
        });

        return app;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(AliveEndpointPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponseAsync,
        });

        app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthResponseAsync,
        });

        return app;
    }

    public static async Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags,
                exception = e.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        }));
    }

    private static bool IsHealthRequest(HttpContext context) =>
        !context.Request.Path.StartsWithSegments(HealthEndpointPath) &&
        !context.Request.Path.StartsWithSegments(AliveEndpointPath);
}
