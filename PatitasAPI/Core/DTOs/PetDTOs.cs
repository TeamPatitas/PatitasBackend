using PatitasAPI.Core.Utils;
namespace PatitasAPI.Core.DTOs;

public record PetResponse(
    Guid Id,
    string Name,
    Species Specie,
    string Breed,
    Gender Gender,
    string Temperament,
    string Story,
    IEnumerable<string> Photos,
    bool Aviable
);