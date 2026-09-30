using AutoMapper;
using Bakery.API.Middleware;
using Bakery.BLL.Interfaces;
using Bakery.BLL.Mapping;
using Bakery.BLL.Services;
using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Конфігурація підключення до бази даних
var connectionString = builder.Configuration.GetConnectionString("BakeryOrdersDB") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=BakeryOrdersDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// Реєстрація інфраструктури доступу до даних (DAL)
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Реєстрація AutoMapper (BLL)
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});
builder.Services.AddSingleton(mapperConfig.CreateMapper());

// Реєстрація сервісів бізнес-логіки (BLL)
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Контролери та Swagger документація
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Централізована обробка помилок у форматі ProblemDetails (404, 409, 400, 500)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
