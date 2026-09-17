using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<UserResponse> GetUserAsync(Guid userId);
    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request);
}