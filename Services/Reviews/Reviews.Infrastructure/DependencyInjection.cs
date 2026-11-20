using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Infrastructure.Initialization;
using Bakery.Reviews.Infrastructure.Persistence;
using Bakery.Reviews.Infrastructure.Queries;
using Bakery.Reviews.Infrastructure.Repositories;
using Bakery.Reviews.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Platform.ServiceDefaults;

namespace Bakery.Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(MongoDbOptions.SectionName);

        services.Configure<MongoDbOptions>(section);

        var options = section.Get<MongoDbOptions>() ?? new MongoDbOptions();

        MongoDbSerialization.Register();

        services.AddSingleton(_ => CreateClient(options, ResolveConnectionString(configuration, options)));
        services.AddSingleton<MongoTransactionState>();

        services.AddScoped<IMongoSessionAccessor, MongoSessionAccessor>();
        services.AddScoped<MongoDbContext>();

        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IProductRatingRepository, ProductRatingRepository>();
        services.AddScoped<IModerationLogRepository, ModerationLogRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();
        services.AddScoped<IMongoUnitOfWork, MongoUnitOfWork>();

        services.AddScoped<MongoIndexInitializer>();
        services.AddScoped<IDataSeeder, MongoDataSeeder>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, MongoDbOptions options)
    {
        var fromHosting = configuration.GetConnectionString("BakeryReviewsDB");

        return string.IsNullOrWhiteSpace(fromHosting) ? options.ConnectionString : fromHosting;
    }

    private static IMongoClient CreateClient(MongoDbOptions options, string connectionString)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);

        settings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(options.ServerSelectionTimeoutMilliseconds);
        settings.ApplicationName = "Bakery.Reviews";

        return new MongoClient(settings.WithOpenTelemetry());
    }
}
