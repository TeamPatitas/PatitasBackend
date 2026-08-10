using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Auth;
using PatitasAPI.Infraestructure.Data;
using PatitasAPI.Infraestructure.Pets;

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

        //Authentication
        services.AddAuthentication(options =>{
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options => {
            var jwtSecret = PatitasEnv.GetEnvVariable("JWT_SECRET_KEY");
            
            var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Scoped Services
        services.AddScoped<IAuthService, IdentityAuthService>();
        services.AddScoped<IPetService, PetsPostgresService>();

        return services;
    }
}