using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure;
namespace PatitasAPI.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Autenticación");

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
        }).WithName("Iniciar Sesión")
        .WithDescription("Iniciar sesión pez")
        .Produces<AuthResponse>(StatusCodes.Status200OK);

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
        }).WithName("Registrarse")
        .WithDescription("Registrarse pez")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .DisableAntiforgery();

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
        }).WithName("Verificar Correo")
        .WithDescription("Verifica el correo del usuario, esta wea está pensada para que se redireccione desde frontend.");

        group.MapGet("/send-verification-email", async (IAuthService authService, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                await authService.SendEmailVerificationAsync(Guid.Parse(userId));
                var frontendUrl = PatitasEnv.GetEnvVariable("FRONTEND_URL");
                return Results.Redirect($"{frontendUrl}/user");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("Enviar Correo de Verificación")
        .WithDescription("Envía un correo de verificación al usuario")
        .RequireAuthorization("User")
        .RequireRateLimiting("EmailVerificationCooldown");
    }
}