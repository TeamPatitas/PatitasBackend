namespace PatitasAPI.Core.DTOs;

public record CreateShelterResponse(
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
    string? PhotoUrl
);