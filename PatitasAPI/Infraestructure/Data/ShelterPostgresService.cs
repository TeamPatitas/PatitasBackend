using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.Infraestructure.Data;

public class ShelterPostgresService(PatitasDbContext context) : IShelterService
{
    private readonly PatitasDbContext _context = context;

    private static CreateShelterResponse ToResponse(Shelter shelter) => new(
        shelter.Id,
        shelter.Name,
        shelter.Address,
        shelter.IsAvailable
    );

    public async Task<CreateShelterResponse> CreateShelterAsync(CreateShelterRequest req)
    {
        var shelter = new Shelter(
            req.Name,
            req.Address
        );

        _context.Shelters.Add(shelter);
        await _context.SaveChangesAsync();
        return ToResponse(shelter);
    }
}