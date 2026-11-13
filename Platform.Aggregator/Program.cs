using Platform.Aggregator.Clients;
using Platform.Aggregator.Diagnostics;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Microsoft.OpenApi;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("Aggregator");

builder.AddServiceDefaults(AggregatorActivitySources.AssemblyName);

builder.Services.AddHttpClient<OrdersClient>(client => client.BaseAddress = new Uri("https+http://orders-api"));
builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog-api"));
builder.Services.AddHttpClient<ReviewsClient>(client => client.BaseAddress = new Uri("https+http://reviews-api"));

builder.Services.AddTransient<OrderAssemblyService>();
builder.Services.AddTransient<ProductOverviewService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Craft Bakery Aggregator API",
    Version = "v1",
    Description = "Композитні read-only ендпоінти над сервісами Orders, Catalog та Reviews: паралельні виклики, часткові відмови та попередження.",
}));

var app = builder.Build();

app.UsePlatformDefaults("Aggregator");
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
