using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.API.Endpoints;

public static class ShelterEndpoints
{
    public static void MapShelterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/shelter").WithTags("Refugios");
        group.MapPost("/", async ([FromForm] CreateShelterRequest request, ClaimsPrincipal user, IShelterService shelterService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var created = await shelterService.CreateShelterAsync(request, userId);
                return Results.Created($"/shelter/{created.Id}", created);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("CreateShelter")
        .WithDescription("Crea un nuevo refugio, es todo xd, luego los devs verán si activan el refugio.")
        .Produces<ShelterResponse>(StatusCodes.Status201Created)
        .RequireAuthorization("User")
        .DisableAntiforgery();

        group.MapPatch("/{id:guid}", async (Guid id, [FromForm] UpdateShelterRequest request, ClaimsPrincipal user, IShelterService shelterService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var result = await shelterService.UpdateAsync(id, request, userId);
                if (result == null) return Results.NotFound(new { message = "Shelter not found" });
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("UpdateShelter")
        .WithDescription("Actualiza los datos de un refugio.")
        .Produces<ShelterResponse?>(StatusCodes.Status200OK)
        .RequireAuthorization("User")
        .DisableAntiforgery();

        group.MapGet("/", async (int? page, int? pageSize, ClaimsPrincipal user, IShelterService shelterService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await shelterService.GetAllAsync(page ?? 1, pageSize ?? 20, userId);
            return Results.Ok(result);
        }).WithName("GetAllShelters")
        .WithDescription("Obtiene todos los refugios.")
        .Produces<PagedResponse<ShelterSummaryResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IShelterService shelterService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await shelterService.GetByIdAsync(id, userId);
            if (result == null) return Results.NotFound();
            return Results.Ok(result);
        }).WithName("GetShelterById")
        .WithDescription("Obtiene los detalles de un refugio por su ID.")
        .Produces<ShelterResponse?>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, IShelterService shelterService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var deleted = await shelterService.DeleteAsync(id, userId);
                if (!deleted) return Results.NotFound(new { message = "Shelter not found" });
                return Results.Ok("Refugio Borrado");
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
        }).WithName("DeleteShelter")
        .WithDescription("CUIDADO: Elimina un refugio, esto es irreversible y elimina todo lo relacionado a ella (adopciones, mascotas) usarlo con cautela, si eres dev tambien puedes eliminarlo a cualquiera 🐒 ")
        .RequireAuthorization("ShelterOwner");
    }
}
