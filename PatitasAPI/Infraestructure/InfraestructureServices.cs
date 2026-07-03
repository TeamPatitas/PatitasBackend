using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Auth;

namespace PatitasAPI.Infraestructure;

public static class InfraestructureServices
{
    public static IServiceCollection AddInfraestructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        //Identity
        services.AddAuthorization(op =>
        {
            op.AddPolicy("DevOnly", policy => policy.RequireRole("Dev"));
            op.AddPolicy("ShelterOwner", policy => policy.RequireRole("Dev", "ShelterOwner"));
            op.AddPolicy("User", policy => policy.RequireRole("Dev", "ShelterOwner", "User"));
        });
        services.AddScoped<IAuthService, IdentityAuthService>();

        return services;
    }
}