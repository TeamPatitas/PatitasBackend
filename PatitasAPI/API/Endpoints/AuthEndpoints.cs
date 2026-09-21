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
         var frontendUrl = PatitasEnv.GetEnvVariable("FRONTEND_URL");


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
        }).WithName("Login")
        .WithDescription("Iniciar sesión pez, el token JWT se renueva cada $bold{20 dias}, osea que cada 20 dias hay que volver a iniciar sesión, guardar el token porque es lo que se va a usar para la mayoría de endpoints.")
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
        }).WithName("Register")
        .WithDescription("Registrarse pez.")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .DisableAntiforgery();

        group.MapGet("/verify-email", async (Guid userId, string token, IAuthService authService) => {
            try
            {
                await authService.VerifyEmailAsync(userId, token);
                return Results.Redirect($"{frontendUrl}/user");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("VerifyEmail")
        .WithDescription("Verifica el correo del usuario, esta wea está pensada para que redireccione a frontend.");

        group.MapGet("/send-verification-email", async (IAuthService authService, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                await authService.SendEmailVerificationAsync(Guid.Parse(userId));
                return Results.Ok($"Se envió un correo de verificación.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("SendVerificationEmail")
        .WithDescription("Envía un correo de verificación al usuario.")
        .RequireAuthorization("User")
        .RequireRateLimiting("EmailVerificationCooldown");
    }
}