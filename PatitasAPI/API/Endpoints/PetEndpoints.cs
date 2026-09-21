using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Core.Utils;
namespace PatitasAPI.API.Endpoints;

public static class PetEndpoints
{
    public static void MapPetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pet").WithTags("Mascotas");

        group.MapPost("/", async ([FromForm] CreatePetRequest request, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var created = await petService.CreatePetAsync(request, userId);
                return Results.Created($"/pet/{created.Id}", created);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("CreatePet")
        .WithDescription("Crea una nueva mascota, es todo xd")
        .Produces<PetResponse>(StatusCodes.Status201Created)
        .RequireAuthorization("ShelterOwner")
        .DisableAntiforgery();

        group.MapGet("/", async (int? page, int? pageSize, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await petService.GetAllPetsAsync(page ?? 1, pageSize ?? 20, userId);
            return Results.Ok(result);
        }).WithName("GetAllPets")
        .WithDescription("Obtiene todas las mascotas.")
        .Produces<PagedResponse<PetSummaryResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        group.MapGet("/{petId:guid}", async (Guid petId, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var petResponse = await petService.GetPetByIdAsync(petId, userId);
            if (petResponse == null)
            {
                return Results.NotFound();
            }
            return Results.Ok(petResponse);
        }).WithName("GetPetById")
        .WithDescription("Obtiene los detalles deuna mascota por su ID.")
        .Produces<PetResponse>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        group.MapPatch("/{petId:guid}", async (Guid petId, UpdatePetRequest request, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var updated = await petService.UpdatePetAsync(petId, request, userId);
                if (updated == null) return Results.NotFound();
                return Results.Ok(updated);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("UpdatePet")
        .WithDescription("Actualiza los datos de una mascota.")
        .Produces<PetResponse?>(StatusCodes.Status200OK)
        .RequireAuthorization("ShelterOwner");

        group.MapPatch("/{petId:guid}/photo/{photoIndex:int}", async (Guid petId, int photoIndex, IFormFile photo, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var updated = await petService.UpdatePetPhotoAsync(petId, photoIndex, photo, userId);
                if (updated == null) return Results.NotFound();
                return Results.Ok(updated);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("UpdatePetPhoto")
        .WithDescription("Actualiza la foto de una mascota, son 3 fotos por mascota y este endpoint reemplaza el especificado.")
        .RequireAuthorization("ShelterOwner")
        .Produces<PetResponse?>(StatusCodes.Status200OK)
        .DisableAntiforgery();

        group.MapDelete("/{petId:guid}", async (Guid petId, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var deleted = await petService.DeletePetAsync(petId, userId);
                if (!deleted) return Results.NotFound();
                return Results.Ok();
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("DeletePet")
        .WithDescription("$important{CUIDADO: Usarlo con cautela}. Elimina una mascota pero también elimina todo lo relacionado a ella, es irreversible.")
        .Produces(StatusCodes.Status200OK)
        .RequireAuthorization("ShelterOwner");
    }
}
