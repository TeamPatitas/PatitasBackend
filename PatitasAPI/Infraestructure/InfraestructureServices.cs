using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Auth;
using PatitasAPI.Infraestructure.Data;

namespace PatitasAPI.Infraestructure;

public static class InfraestructureServices
{
    public static IServiceCollection AddInfraestructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        //PostgresDB
        services.AddDbContext<PatitasDbContext>(options => options.UseNpgsql(PatitasEnv.GetDbConnection()));

        //Identity
        services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<PatitasDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthorizationBuilder()
            .AddPolicy("DevOnly", policy => policy.RequireRole("Dev"))
            .AddPolicy("ShelterOwner", policy => policy.RequireRole("Dev", "ShelterOwner"))
            .AddPolicy("User", policy => policy.RequireRole("Dev", "ShelterOwner", "User"));

        services.AddScoped<IAuthService, IdentityAuthService>();

        return services;
    }
}