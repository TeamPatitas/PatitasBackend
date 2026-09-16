namespace PatitasAPI.Core.DTOs;

public record ShelterResponse(
    Guid Id,
    string Name,
    string Address,
    bool IsAvailable,
    double? Latitude = null,
    double? Longitude = null,
    string? PhotoUrl = null
);

public record CreateShelterRequest(
    string Name,
    string Address,
    double? Latitude,
    double? Longitude,
    IFormFile? Photo = null
);

public record UpdateShelterRequest(
    string? Name,
    string? Address,
    double? Latitude,
    double? Longitude,
    IFormFile? Photo = null
);