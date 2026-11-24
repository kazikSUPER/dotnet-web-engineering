using Auth.Api.Data;
using Auth.Api.Endpoints;
using Auth.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Auth;
using Platform.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("Auth.Api");
builder.AddServiceDefaults();

// 1. Separate IdentityDb database via Aspire connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb"),
        sql => sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)));

// 1. ASP.NET Identity with password & lockout configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // 4. Strict requirement: login blocked without confirmed email
    options.SignIn.RequireConfirmedEmail = true;

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddPlatformAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure DB created and seed roles & admin user
await InitializeIdentityDatabaseAsync(app);

app.UsePlatformDefaults("Auth.Api");
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapDefaultEndpoints();

await app.RunAsync();

static async Task InitializeIdentityDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

    try
    {
        await db.Database.MigrateAsync();

        string[] roles = ["Admin", "Manager", "User", "Guest"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed default Admin user
        const string adminEmail = "admin@platform.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Seeded default Admin user: {Email}", adminEmail);
            }
        }

        // Seed default Student user
        const string studentEmail = "student@bakery.com";
        var student = await userManager.FindByEmailAsync(studentEmail);
        if (student is null)
        {
            student = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FirstName = "Студент",
                LastName = "ЧНУ",
                EmailConfirmed = true
            };
            var studentResult = await userManager.CreateAsync(student, "Password123!");
            if (studentResult.Succeeded)
            {
                await userManager.AddToRoleAsync(student, "User");
                logger.LogInformation("Seeded default Student user: {Email}", studentEmail);
            }
        }
        else if (!student.EmailConfirmed)
        {
            student.EmailConfirmed = true;
            await userManager.UpdateAsync(student);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize Identity database");
    }
}
