using System.Security.Claims;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.API.Endpoints;

public static class ShelterEndpoints
{
    public static void MapShelterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/shelter").WithTags("Refugios");
        group.MapPost("/", async (CreateShelterRequest request, ClaimsPrincipal user, IShelterService shelterService) =>
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
        }).WithName("CreateShelter").RequireAuthorization("User");

        group.MapPatch("/enable/{id:guid}", async (Guid id, IShelterService shelterService) =>
        {
            try
            {
                var result = await shelterService.EnableAsync(id);
                if (result == null) return Results.NotFound(new { message = "Shelter not found" });
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("EnableShelter").RequireAuthorization("DevOnly");

        group.MapPatch("/disable/{id:guid}", async (Guid id, IShelterService shelterService) =>
        {
            try
            {
                var result = await shelterService.DisableAsync(id);
                if (result == null) return Results.NotFound(new { message = "Shelter not found" });
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("DisableShelter").RequireAuthorization("DevOnly");

        group.MapGet("/", async (int? page, int? pageSize, ClaimsPrincipal user, IShelterService shelterService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await shelterService.GetAllAsync(page ?? 1, pageSize ?? 20, userId);
            return Results.Ok(result);
        }).WithName("GetAllShelters").RequireAuthorization("User");
    }
}
