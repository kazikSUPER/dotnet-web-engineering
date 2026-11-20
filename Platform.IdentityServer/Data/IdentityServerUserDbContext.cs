using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Platform.IdentityServer.Data;

public class IdentityServerUserDbContext(DbContextOptions<IdentityServerUserDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
}
