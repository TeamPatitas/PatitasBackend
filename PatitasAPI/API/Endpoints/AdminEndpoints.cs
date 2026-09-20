using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Seeders;

namespace PatitasAPI.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin").WithTags("Administración");

        var healthEndpoint = group.MapGet("/health", async (IHealthService healthService) => {
            var result = await healthService.CheckHealthAsync();
            return result.Status == "UP" ? Results.Ok(result) : Results.Json(result, statusCode: 503);
        }).WithName("HealthCheck")
        .WithDescription("Verifica el estado de la api y sus dependencias, es piola pa saber que c cayó xd")
        .Produces<HealthResponse>()
        .RequireAuthorization("DevOnly")
        .RequireRateLimiting("HealthCheckCooldown");

        // User endpoints - mantienen permisos User
        group.MapGet("/user", async (ClaimsPrincipal user, IAuthService authService) => {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var response = await authService.GetUserAsync(Guid.Parse(userId));
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("GetCurrentUser")
        .WithDescription("Obtiene la información del usuario actual, necesita que el usuario haya iniciado sesión.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        group.MapPatch("/user", async ([FromForm] UpdateUserRequest request, ClaimsPrincipal user, IAuthService authService) => {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var response = await authService.UpdateUserAsync(Guid.Parse(userId), request);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("UpdateCurrentUser")
        .WithDescription("Actualiza la información del usuario, necesita que el usuario haya iniciado sesión, pero si tienes rol dev puedes modificar cualquier cosa a cualquiera xd.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .RequireAuthorization("User")
        .DisableAntiforgery();

        group.MapDelete("/user/{id:guid}", async (Guid id, ClaimsPrincipal user, IAuthService authService) => {
            var requesterId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (requesterId == null) return Results.Unauthorized();
            try
            {
                var deleted = await authService.DeleteUserAsync(id, Guid.Parse(requesterId));
                if (!deleted) return Results.NotFound(new { message = "Usuario no encontrado" });
                return Results.Ok("Usuario borrado");
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("Borrar Usuario")
        .WithDescription("CUIDADO Borra el usuario con el ID especificado, esto es irreversible usarlo con mucha cautela, si eres dev tambien puedes eliminarlo a cualquiera 🐒 ")
        .Produces<bool>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        // Shelter
        var shelterGroup = group.MapGroup("/shelter");
        shelterGroup.MapPatch("/enable/{id:guid}", async (Guid id, IShelterService shelterService) =>
        {
            try
            {
                var result = await shelterService.EnableAsync(id);
                if (result == null) return Results.NotFound(new { message = "Shelter not found" });
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("EnableShelter")
        .WithDescription("Habilita un shelter con el ID especificado, hace que el refugio aparezca para todos los usuarios y luego asigna el rol ShelterOwner a todos los dueños del refugio.")
        .Produces<ShelterResponse?>(StatusCodes.Status200OK)
        .RequireAuthorization("DevOnly")
        .RequireRateLimiting("ShelterSwitchAviabilityCooldown");

        shelterGroup.MapPatch("/disable/{id:guid}", async (Guid id, IShelterService shelterService) =>
        {
            try
            {
                var result = await shelterService.DisableAsync(id);
                if (result == null) return Results.NotFound(new { message = "Shelter not found" });
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("DisableShelter")
        .WithDescription("Deshabilita un shelter con el ID especificado, hace que el refugio no aparezca para los usuarios y luego remueve el rol ShelterOwner de todos los dueños del refugio.")
        .Produces<ShelterResponse?>(StatusCodes.Status200OK)
        .RequireAuthorization("DevOnly")
        .RequireRateLimiting("ShelterSwitchAviabilityCooldown");

        group.MapPatch("/add-roles", async (SwitchRolesRequest req, IAuthService authService) =>
        {
            try
            {
                await authService.AddUserRolesAsync(req);
                return Results.Ok("Roles agregados");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("AddRoles")
        .WithDescription("Agrega roles a un usuario. El nombre lo dice todo 🤌")
        .RequireAuthorization("DevOnly");

        group.MapPatch("/remove-roles", async (SwitchRolesRequest req, IAuthService authService) =>
        {
            try
            {
                await authService.RemoveRolesAsync(req);
                return Results.Ok("Roles removidos");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("RemoveRoles")
        .WithDescription("Remueve roles de un usuario. El nombre lo dice todo 🤌")
        .RequireAuthorization("DevOnly");

        group.MapGet("/users", async (int? page, int? pageSize, IAuthService authService) =>
        {
            try
            {
                var result = await authService.GetAllUsersAsync(page ?? 1, pageSize ?? 20);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("GetAllUsers")
        .WithDescription("Obtiene todos los usuarios.")
        .Produces<PagedResponse<UserSummaryResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization("DevOnly");
        
        // Dev seed
        if(app.Environment.IsDevelopment())
        {
            var devGroup = app.MapGroup("/dev").WithTags("Development & Testing");
            devGroup.MapPost("/seedUsers", async (UserManager<AppUser> userManager) =>
            {
                try {
                    await AuthSeeder.SeedDefaultUserAsync(userManager);
                    return Results.Ok(new { message = "Usuarios de prueba creados exitosamente." });
                } catch (Exception ex) {
                    return Results.Problem($"Error al crear usuarios de prueba: {ex.Message}");
                }
            }).WithName("Seeding").RequireAuthorization("DevOnly");
        }
       
    }
}
