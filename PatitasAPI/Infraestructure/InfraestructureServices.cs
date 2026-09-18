using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Auth;
using PatitasAPI.Infraestructure.Data;
using PatitasAPI.Infraestructure.Email;
using PatitasAPI.Infraestructure.Health;
using PatitasAPI.Infraestructure.Storage;

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
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        //Cooldowns
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("EmailVerificationCooldown", httpContext => {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp, 
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1, 
                        Window = TimeSpan.FromMinutes(1), 
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }
                );
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Espera 1 minuto antes de volver a solicitar la verificación." }, 
                    cancellationToken);
            };
        });

        //R3 Storage
        services.AddScoped<IStorageService, CloudflareR2Service>();

        //Email Service
        services.AddHttpClient<IEmailService, ResendEmailService>();

        // Scoped Services
        services.AddScoped<IAuthService, IdentityAuthService>();
        services.AddScoped<IPetService, PetsPostgresService>();
        services.AddScoped<IShelterService, ShelterPostgresService>();
        services.AddScoped<IHealthService, HealthService>();

        return services;
    }
}