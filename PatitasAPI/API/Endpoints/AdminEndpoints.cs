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

        group.MapGet("/health", async (IHealthService healthService) => {
            var result = await healthService.CheckHealthAsync();
            return result.Status == "UP" ? Results.Ok(result) : Results.Json(result, statusCode: 503);
        }).WithName("HealthCheck").RequireAuthorization("DevOnly").RequireRateLimiting("HealthCheckCooldown");

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
        }).WithName("GetCurrentUser").RequireAuthorization("User");

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
        }).WithName("UpdateCurrentUser").RequireAuthorization("User").DisableAntiforgery();

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
        }).WithName("Borrar Usuario").RequireAuthorization("User");

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
        }).WithName("EnableShelter").RequireAuthorization("DevOnly").RequireRateLimiting("ShelterSwitchAviabilityCooldown");

        shelterGroup.MapPatch("/disable/{id:guid}", async (Guid id, IShelterService shelterService) =>
        {
            try
            {
                var result = await shelterService.DisableAsync(id);
                if (result == null) return Results.NotFound(new { message = "Shelter not found" });
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("DisableShelter").RequireAuthorization("DevOnly").RequireRateLimiting("ShelterSwitchAviabilityCooldown");

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
        }).WithName("AddRoles").RequireAuthorization("DevOnly");

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
        }).WithName("RemoveRoles").RequireAuthorization("DevOnly");
        
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
