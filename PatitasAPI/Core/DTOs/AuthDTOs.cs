namespace PatitasAPI.Core.DTOs;
public record LoginRequest(string Email, string Password);
public record RegisterRequest(
    string FirstName, 
    string LastName,
    string Email, 
    string Password,
    DateOnly BirthDate,
    int Gender
);
public record AuthResponse(string Token, List<string> Roles);
public record UserResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    int Gender,
    string PhotoUrl,
    DateOnly BirthDate,
    string Role,
    Guid? ShelterId 
);