namespace PatitasAPI.Core.DTOs;
public record CreateEventRequest {
    public required string Name { get; init; }
    public required DateTime EventDate { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? Description { get; init; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public bool? IsActive { get; set; }
    public IFormFile? Image { get; init; }
};

public record UpdateEventRequest {
    public string? Name { get; init; }
    public DateTime? EventDate { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? Description { get; init; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public bool? IsActive { get; set; }
    public IFormFile? Image { get; init; }
};

public record EventResponse {
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime EventDate { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? Description { get; init; }
    public string? Latitude { get; init; }
    public string? Longitude { get; init; }
    public string? PhotoUrl { get; init; }
    public required bool IsActive { get; init; }
    public required Guid ShelterId { get; init; }
    public required bool IsYours { get; init; }
};

public record EventSummaryResponse {
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime EventDate { get; init; }
    public string? PhotoUrl { get; init; }
    public required bool IsActive { get; init; }
    public required Guid ShelterId { get; init; }
    public required bool IsYours { get; init; }
};