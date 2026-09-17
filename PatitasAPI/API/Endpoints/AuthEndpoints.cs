using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Interfaces;
namespace PatitasAPI.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/user", async (ClaimsPrincipal user, IAuthService authService) =>
        {
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
        }).WithName("GetCurrentUser").RequireAuthorization("User").WithTags("Authentication");

        app.MapPatch("/user", async ([FromForm] UpdateUserRequest request, ClaimsPrincipal user, IAuthService authService) =>
        {
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
        }).WithName("UpdateCurrentUser").RequireAuthorization("User").WithTags("Authentication").DisableAntiforgery();

        // Auth
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

        group.MapPost("/register", async ([FromForm] RegisterRequest request, IAuthService authService) =>
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
        }).WithName("Registrarse").DisableAntiforgery();

    }
}