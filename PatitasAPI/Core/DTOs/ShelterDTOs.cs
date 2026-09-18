namespace PatitasAPI.Core.DTOs;

public record ShelterResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required bool IsAvailable { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? PhotoUrl { get; init; }
};

public record CreateShelterRequest
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IFormFile? Photo { get; init; }
};

public record UpdateShelterRequest
{
    public string? Name { get; init; }
    public string? Address { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IFormFile? Photo { get; init; }
};