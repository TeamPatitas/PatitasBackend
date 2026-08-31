using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Data;
namespace PatitasAPI.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // GET /user en dominio base (fuera de /auth)
        app.MapGet("/user", async (ClaimsPrincipal user, UserManager<AppUser> userManager, PatitasDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var appUser = await db.Users.Include(u => u.Shelter).FirstOrDefaultAsync(u => u.Id == userId);
            if (appUser == null) return Results.NotFound(new { message = "Usuario no encontrado" });
            var roles = await userManager.GetRolesAsync(appUser);
            var role = roles.FirstOrDefault() ?? "User";
            var response = new UserResponse(
                appUser.Id,
                appUser.FirstName,
                appUser.LastName,
                appUser.Email ?? "",
                (int)appUser.Gender,
                appUser.PhotoUrl ?? "",
                appUser.BirthDate,
                role,
                appUser.ShelterId
            );
            return Results.Ok(response);
        }).WithName("GetCurrentUser").RequireAuthorization("User");

        var group = app.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            try
            {
                var response = await authService.LoginAsync(request);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("Iniciar Sesión");

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            try
            {
                var response = await authService.RegisterAsync(request);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("Registrarse");
    }
}