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
                var created = await shelterService.CreateShelterAsync(request);
                return Results.Created($"/shelter/{created.Id}", created);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("CreateShelter").RequireAuthorization("User");
    }
}