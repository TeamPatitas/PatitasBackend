using PatitasAPI.Core.Interfaces;
namespace PatitasAPI.API.Endpoints;

public static class PetEndpoints
{
    public static void MapPetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pet").WithTags("Mascotas");

       group.MapGet("/{petId:guid}", async (Guid petId, IPetService petService) =>
        {
            var petResponse = await petService.GetPetByIdAsync(petId);
            if (petResponse == null)
            {
                return Results.NotFound();
            }
            return Results.Ok(petResponse);
        }).WithName("GetPetById").RequireAuthorization("User");
    }
}