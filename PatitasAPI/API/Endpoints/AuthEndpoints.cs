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
        }).WithName("GetCurrentUser").RequireAuthorization("User");

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
        }).WithName("UpdateCurrentUser").RequireAuthorization("User").DisableAntiforgery();

        app.MapDelete("/user/{id:guid}", async (Guid id, ClaimsPrincipal user, IAuthService authService) =>
        {
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

        group.MapGet("/verify-email", async (Guid userId, string token, IAuthService authService) => {
            try
            {
                await authService.VerifyEmailAsync(userId, token);
                return Results.Ok("Correo verificado correctamente");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("Verificar Correo");

        group.MapGet("/send-verification-email", async (IAuthService authService, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                await authService.SendEmailVerificationAsync(Guid.Parse(userId));
                return Results.Ok("Correo de verificación enviado correctamente");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("Enviar Correo de Verificación").RequireAuthorization("User").RequireRateLimiting("EmailVerificationCooldown");
    }
}