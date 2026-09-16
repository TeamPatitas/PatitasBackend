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

        group.MapPost("/", async (
            [FromForm] string Name,
            [FromForm] Species Species,
            [FromForm] string Breed,
            [FromForm] Gender Gender,
            [FromForm] string Temperament,
            [FromForm] string Story,
            [FromForm] bool? Available,
            [FromForm] List<IFormFile>? Photos,
            ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var request = new CreatePetRequest(Name, Species, Breed, Gender, Temperament, Story, Available);
                var created = await petService.CreatePetAsync(request, Photos, userId);
                return Results.Created($"/pet/{created.Id}", created);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("CreatePet").RequireAuthorization("ShelterOwner").DisableAntiforgery();

        group.MapGet("/", async (int? page, int? pageSize, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await petService.GetAllPetsAsync(page ?? 1, pageSize ?? 20, userId);
            return Results.Ok(result);
        }).WithName("GetAllPets").RequireAuthorization("User");

        group.MapGet("/{petId:guid}", async (Guid petId, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var petResponse = await petService.GetPetByIdAsync(petId, userId);
            if (petResponse == null)
            {
                return Results.NotFound();
            }
            return Results.Ok(petResponse);
        }).WithName("GetPetById").RequireAuthorization("User");

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
        }).WithName("UpdatePet").RequireAuthorization("ShelterOwner");

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
        }).WithName("UpdatePetPhoto").RequireAuthorization("ShelterOwner").DisableAntiforgery();

        group.MapDelete("/{petId:guid}", async (Guid petId, ClaimsPrincipal user, IPetService petService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var deleted = await petService.DeletePetAsync(petId, userId);
                if (!deleted) return Results.NotFound();
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("DeletePet").RequireAuthorization("ShelterOwner");
    }
}
