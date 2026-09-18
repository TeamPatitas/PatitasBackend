using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<UserResponse> GetUserAsync(Guid userId);
    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task SendEmailVerificationAsync(Guid userId);
    Task VerifyEmailAsync(Guid userId, string token);
    Task<bool> DeleteUserAsync(Guid targetUserId, Guid requesterUserId);
}