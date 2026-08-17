using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Core.Utils;
using PatitasAPI.Infraestructure.Data;

namespace PatitasAPI.Infraestructure.Pets;

public class PetsPostgresService(PatitasDbContext dbContext) : IPetService
{
    private readonly PatitasDbContext _dbContext = dbContext;

    public async Task<PetResponse?> GetPetByIdAsync(Guid petId)
    {
        var pet = new Pet(
            name: "Firulais",
            species: Species.DOG,
            breed: "Mestizo",
            gender: Gender.MALE,
            temperament: "Muy juguetón y leal. Ideal para familias con niños.",
            story: "Fue encontrado vagando cerca del mercado central. Le encantan las pelotas.",
            photos: ["https://picsum.photos/seed/firulais/400/300"],
            aviable: true 
        );

        return new PetResponse(
            pet.Id,
            pet.Name,
            pet.Species,
            pet.Breed,
            pet.Gender,
            pet.Temperament,
            pet.Story,
            pet.Photos,
            pet.Aviable
        );
    }
}