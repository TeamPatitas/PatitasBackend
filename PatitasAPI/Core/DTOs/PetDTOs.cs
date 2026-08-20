using PatitasAPI.Core.Utils;
namespace PatitasAPI.Core.DTOs;

public record CreatePetResponse(
    Guid Id,
    string Name,
    Species Specie,
    string Breed,
    Gender Gender,
    string Temperament,
    string Story,
    IEnumerable<string> Photos,
    bool Available,
    Guid ShelterId
);

public record CreatePetRequest(
    string Name,
    Species Species,
    string Breed,
    Gender Gender,
    string Temperament,
    string Story,
    List<string> Photos,
    bool Available
);

public record UpdatePetRequest(
    string? Name,
    Species? Species,
    string? Breed,
    Gender? Gender,
    string? Temperament,
    string? Story,
    List<string>? Photos,
    bool? Available
);