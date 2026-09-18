namespace PatitasAPI.Core.DTOs;

public record PagedResponse<T>
{
    public required IEnumerable<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
};

public record HealthServiceResult
{
    public required string Status { get; init; }
    public required double LatencyMs { get; init; }
    public string? Error { get; init; }
};

public record HealthResponse
{
    public required string Status { get; init; }
    public required Dictionary<string, HealthServiceResult> Services { get; init; }
};