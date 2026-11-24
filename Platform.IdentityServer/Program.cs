using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.IdentityServer;
using Platform.IdentityServer.Data;
using Platform.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("IdentityServer");
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityServerDb")
    ?? "Server=(localdb)\\mssqllocaldb;Database=IdentityServerDb;Trusted_Connection=True;MultipleActiveResultSets=true";
var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

// 7. Identity Server separate database & ASP.NET Identity
builder.Services.AddDbContext<IdentityServerUserDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<IdentityServerUserDbContext>()
.AddDefaultTokenProviders();

// 7. IdentityServer with ConfigurationStore and OperationalStore
var isBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    options.EmitStaticAudienceClaim = true;
})
.AddAspNetIdentity<IdentityUser>()
.AddConfigurationStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
})
.AddOperationalStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
    options.EnableTokenCleanup = true;
})
.AddDeveloperSigningCredential();

builder.Services.AddControllersWithViews();

var app = builder.Build();

await InitializeDatabaseAsync(app);

app.UsePlatformDefaults("IdentityServer");
app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapDefaultEndpoints();

await app.RunAsync();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityServer.Init");

    try
    {
        var userDb = scope.ServiceProvider.GetRequiredService<IdentityServerUserDbContext>();
        try { await userDb.Database.MigrateAsync(); } catch { await userDb.Database.EnsureCreatedAsync(); }

        var configDb = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        try { await configDb.Database.MigrateAsync(); } catch { await configDb.Database.EnsureCreatedAsync(); }

        var persistedDb = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
        try { await persistedDb.Database.MigrateAsync(); } catch { await persistedDb.Database.EnsureCreatedAsync(); }

        // Seed Identity Resources
        if (!await configDb.IdentityResources.AnyAsync())
        {
            foreach (var resource in Config.IdentityResources)
            {
                configDb.IdentityResources.Add(resource.ToEntity());
            }
            await configDb.SaveChangesAsync();
        }

        // Seed Api Scopes
        if (!await configDb.ApiScopes.AnyAsync())
        {
            foreach (var scopeItem in Config.ApiScopes)
            {
                configDb.ApiScopes.Add(scopeItem.ToEntity());
            }
            await configDb.SaveChangesAsync();
        }

        // Seed Api Resources
        if (!await configDb.ApiResources.AnyAsync())
        {
            foreach (var resource in Config.ApiResources)
            {
                configDb.ApiResources.Add(resource.ToEntity());
            }
            await configDb.SaveChangesAsync();
        }

        // Seed Clients
        if (!await configDb.Clients.AnyAsync())
        {
            foreach (var client in Config.Clients)
            {
                configDb.Clients.Add(client.ToEntity());
            }
            await configDb.SaveChangesAsync();
        }

        // Seed Roles and Test Users
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        string[] roles = ["Admin", "Manager", "User", "Guest"];
        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
            {
                await roleManager.CreateAsync(new IdentityRole(r));
            }
        }

        if (await userManager.FindByEmailAsync("admin@platform.local") is null)
        {
            var admin = new IdentityUser { UserName = "admin@platform.local", Email = "admin@platform.local", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (await userManager.FindByEmailAsync("user@platform.local") is null)
        {
            var user = new IdentityUser { UserName = "user@platform.local", Email = "user@platform.local", EmailConfirmed = true };
            await userManager.CreateAsync(user, "User123!");
            await userManager.AddToRoleAsync(user, "User");
        }

        logger.LogInformation("IdentityServer databases and configuration seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize IdentityServer database");
    }
}
