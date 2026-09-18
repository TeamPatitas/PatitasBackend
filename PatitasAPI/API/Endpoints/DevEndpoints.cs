using Microsoft.AspNetCore.Identity;
using PatitasAPI.Core.Entities;
using PatitasAPI.Infraestructure.Seeders;
namespace PatitasAPI.API.Endpoints;

public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/dev").WithTags("Development & Testing");

        group.MapPost("/seedUsers", async (UserManager<AppUser> userManager) =>
        {
           try {
                await AuthSeeder.SeedDefaultUserAsync(userManager);
                return Results.Ok(new { message = "Usuarios de prueba creados exitosamente." });
            }catch (Exception ex) {
                return Results.Problem($"Error al crear usuarios de prueba: {ex.Message}");
            }
        }).WithName("Seeding");
    }
}