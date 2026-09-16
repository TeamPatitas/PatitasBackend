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
    bool? Available = false,
    List<IFormFile>? Photos = null
);

public record UpdatePetRequest(
    string? Name,
    Species? Species,
    string? Breed,
    Gender? Gender,
    string? Temperament,
    string? Story,
    bool? Available
);

public record UpdatePetPhotoRequest(
    int PhotoIndex,
    IFormFile Photo
);