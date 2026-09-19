using PatitasAPI.Core.Utils;
namespace PatitasAPI.Core.DTOs;

public record PetResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Species Specie { get; init; }
    public required string Breed { get; init; }
    public required Gender Gender { get; init; }
    public required string Temperament { get; init; }
    public required string Story { get; init; }
    public required IEnumerable<string> Photos { get; init; }
    public required bool Available { get; init; }
    public required Guid ShelterId { get; init; }
};

public record PetSummaryResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IEnumerable<string> Photos { get; init; }
    public required bool Available { get; init; }
};

public record CreatePetRequest
{
    public required string Name { get; init; }
    public required Species Species { get; init; }
    public required string Breed { get; init; }
    public required Gender Gender { get; init; }
    public required string Temperament { get; init; }
    public required string Story { get; init; }
    public bool? Available { get; init; } = false;
    public IList<IFormFile>? Photos { get; init; }
};

public record UpdatePetRequest
{
    public string? Name { get; init; }
    public Species? Species { get; init; }
    public string? Breed { get; init; }
    public Gender? Gender { get; init; }
    public string? Temperament { get; init; }
    public string? Story { get; init; }
    public bool? Available { get; init; }
};

public record UpdatePetPhotoRequest
{
    public required int PhotoIndex { get; init; }
    public required IFormFile Photo { get; init; }
};