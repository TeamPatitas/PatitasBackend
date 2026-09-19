namespace PatitasAPI.Core.DTOs;
public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
};
public record RegisterRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required int Gender { get; init; }
    public IFormFile? Photo { get; init; }
};

public record UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public int? Gender { get; init; }
    public IFormFile? Photo { get; init; }
};
public record AuthResponse
{
    public required string Token { get; init; }
    public required IEnumerable<string> Roles { get; init; }
};
public record UserResponse
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required bool IsEmailConfirmed { get; init; }
    public required int Gender { get; init; }
    public required string PhotoUrl { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required IEnumerable<string> Roles { get; init; }
    public Guid? ShelterId { get; init; }
};

public record UserSummaryResponse
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required IEnumerable<string> Roles { get; init; }
}

public record SwitchRolesRequest
{
    public required Guid UserId { get; init; }
    public required IList<string> Roles { get; init; }
};